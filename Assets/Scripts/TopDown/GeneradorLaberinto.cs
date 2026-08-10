using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Genera un laberinto en runtime y lo pinta sobre los Tilemaps de Suelo y Paredes.
///
/// El algoritmo es un "recursive backtracker" hecho con pila explicita (sin
/// recursion, asi no revienta el stack en laberintos grandes). Produce un
/// laberinto perfecto: existe un unico camino entre dos puntos cualesquiera,
/// no hay bucles ni atajos. Eso es lo que obliga al jugador a devolverse.
///
/// El laberinto vive en dos escalas:
///   - "celdas": la reticula logica del algoritmo (celdasAncho x celdasAlto)
///   - "tiles":  lo que se pinta. Cada celda ocupa anchoPasillo tiles y entre
///               celda y celda hay grosorMuro tiles de piedra.
///
/// Los muros usan el sistema de BORDES del tileset: cada tile de muro lleva
/// su pedacito de piso incluido, y la pieza correcta depende de hacia donde
/// queda el piso. El script lo resuelve mirando los ocho vecinos de cada
/// celda, igual que haria un Rule Tile.
///
/// OJO: Generar() borra todo lo que haya en los dos Tilemaps. La decoracion
/// hecha a mano debe ir en un tercer Tilemap que este script no toca.
/// </summary>
public class GeneradorLaberinto : MonoBehaviour
{
    [Header("Tilemaps a pintar")]
    [SerializeField] private Tilemap suelo;
    [SerializeField] private Tilemap paredes;

    [Header("Piso")]
    [Tooltip("Piso liso. Dungeon_walls_33.")]
    [SerializeField] private TileBase tilePiso;

    [Tooltip("Pisos con piedritas y huesos. Dungeon_walls_0 a 21.")]
    [SerializeField] private TileBase[] tilesPisoDecorado;

    [Range(0f, 0.4f)]
    [SerializeField] private float probabilidadDecoracion = 0.07f;

    [Header("Bordes rectos (se elige una variante al azar)")]
    [Tooltip("Muro con piso ABAJO. La piedra va en el borde superior del tile.")]
    [SerializeField] private TileBase[] tilesBordeArriba;

    [Tooltip("Muro con piso ARRIBA.")]
    [SerializeField] private TileBase[] tilesBordeAbajo;

    [Tooltip("Muro con piso a la DERECHA.")]
    [SerializeField] private TileBase[] tilesBordeIzquierda;

    [Tooltip("Muro con piso a la IZQUIERDA.")]
    [SerializeField] private TileBase[] tilesBordeDerecha;

    [Header("Esquinas concavas (el piso rodea dos lados)")]
    [SerializeField] private TileBase tileConcavaNE;
    [SerializeField] private TileBase tileConcavaNO;
    [SerializeField] private TileBase tileConcavaSE;
    [SerializeField] private TileBase tileConcavaSO;

    [Header("Esquinas convexas (solo la diagonal tiene piso)")]
    [SerializeField] private TileBase tileConvexaSE;
    [SerializeField] private TileBase tileConvexaSO;
    [SerializeField] private TileBase tileConvexaNE;
    [SerializeField] private TileBase tileConvexaNO;

    [Header("Relleno")]
    [Tooltip("Celdas de muro que no tocan piso por ningun lado. Piedra maciza.")]
    [SerializeField] private TileBase[] tilesMuroInterior;

    [Header("Tamano del laberinto")]
    [Tooltip("El laberinto mide celdasAncho*(anchoPasillo+grosorMuro)+grosorMuro tiles de ancho.")]
    [SerializeField] private int celdasAncho = 7;
    [SerializeField] private int celdasAlto = 5;

    [Tooltip("Ancho de los pasillos en tiles.")]
    [SerializeField] private int anchoPasillo = 2;

    [Tooltip("Grosor de los muros en tiles. Minimo 2 para que las esquinas cierren bien.")]
    [SerializeField] private int grosorMuro = 2;

    [Header("Ubicacion")]
    [Tooltip("Centra el laberinto en la posicion de este GameObject.")]
    [SerializeField] private bool centrarEnEsteObjeto = true;

    [Tooltip("Solo se usa si NO esta marcado 'centrar'. Esquina inferior izquierda, en celdas del Tilemap.")]
    [SerializeField] private Vector2Int origen = Vector2Int.zero;

    [Header("Aleatoriedad")]
    [SerializeField] private bool semillaAleatoria = true;

    [Tooltip("Con la misma semilla sale exactamente el mismo laberinto.")]
    [SerializeField] private int semilla = 12345;

    [Header("Opcional")]
    [Tooltip("Si lo asignas, se teletransporta a la entrada al generar.")]
    [SerializeField] private Transform jugador;

    [SerializeField] private bool generarAlIniciar = true;

    // --- resultados que otros scripts pueden leer ---

    /// <summary>Centro del pasillo de entrada, en coordenadas de mundo.</summary>
    public Vector3 PosicionInicio { get; private set; }

    /// <summary>Centro del pasillo de salida, en coordenadas de mundo.</summary>
    public Vector3 PosicionMeta { get; private set; }

    /// <summary>Todas las celdas caminables, en mundo. Para spawnear cosas.</summary>
    public List<Vector3> CeldasCaminables { get; } = new List<Vector3>();

    /// <summary>Rectangulo que ocupa el laberinto en el mundo. Para limitar la camara.</summary>
    public Bounds LimitesMundo { get; private set; }

    /// <summary>True cuando ya termino de generar. Otros scripts deben esperar esto.</summary>
    public bool Listo { get; private set; }

    /// <summary>Se dispara al terminar de generar, por si alguien necesita reaccionar.</summary>
    public event System.Action AlGenerar;

    // --- estado interno ---

    private System.Random rng;
    private bool[,] paredEste;   // pared entre (x,y) y (x+1,y)
    private bool[,] paredSur;    // pared entre (x,y) y (x,y+1)
    private bool[,] esPiso;      // grilla de tiles. OJO: y crece hacia ABAJO
    private int anchoTiles, altoTiles;
    private Vector2Int origenReal;

    private void Start()
    {
        if (generarAlIniciar) Generar();
    }

    /// <summary>
    /// Ajusta el tamano segun el nivel y regenera. Cada nivel agrega 2 celdas
    /// por lado, asi que el laberinto crece y se enreda mas sin tocar nada mas.
    /// </summary>
    public void GenerarNivel(int nivel)
    {
        int extra = Mathf.Max(0, nivel - 1) * 2;
        celdasAncho += extra;
        celdasAlto += extra;
        Generar();
    }

    [ContextMenu("Regenerar")]
    public void Generar()
    {
        if (suelo == null || paredes == null)
        {
            Debug.LogError("[GeneradorLaberinto] Faltan los Tilemaps de Suelo o Paredes.", this);
            return;
        }
        if (tilePiso == null)
        {
            Debug.LogError("[GeneradorLaberinto] Falta el tile de piso. Usa 'Autocompletar tiles'.", this);
            return;
        }

        celdasAncho = Mathf.Max(2, celdasAncho);
        celdasAlto = Mathf.Max(2, celdasAlto);
        anchoPasillo = Mathf.Max(1, anchoPasillo);
        grosorMuro = Mathf.Max(1, grosorMuro);

        rng = new System.Random(semillaAleatoria ? System.Environment.TickCount : semilla);

        ExcavarCeldas();
        ConstruirGrillaDeTiles();
        CalcularOrigen();
        Pintar();
        CalcularPuntosDeInteres();

        if (jugador != null) jugador.position = PosicionInicio;

        Listo = true;
        AlGenerar?.Invoke();
    }

    // ------------------------------------------------------------------
    // 1. Backtracker sobre la reticula de celdas
    // ------------------------------------------------------------------
    private void ExcavarCeldas()
    {
        paredEste = new bool[celdasAncho, celdasAlto];
        paredSur = new bool[celdasAncho, celdasAlto];
        bool[,] visitada = new bool[celdasAncho, celdasAlto];

        for (int x = 0; x < celdasAncho; x++)
            for (int y = 0; y < celdasAlto; y++)
            { paredEste[x, y] = true; paredSur[x, y] = true; }

        var pila = new Stack<Vector2Int>();
        var actual = new Vector2Int(0, 0);
        visitada[0, 0] = true;
        pila.Push(actual);

        var vecinos = new List<Vector2Int>(4);

        while (pila.Count > 0)
        {
            actual = pila.Peek();

            vecinos.Clear();
            if (actual.x > 0 && !visitada[actual.x - 1, actual.y]) vecinos.Add(new Vector2Int(-1, 0));
            if (actual.x < celdasAncho - 1 && !visitada[actual.x + 1, actual.y]) vecinos.Add(new Vector2Int(1, 0));
            if (actual.y > 0 && !visitada[actual.x, actual.y - 1]) vecinos.Add(new Vector2Int(0, -1));
            if (actual.y < celdasAlto - 1 && !visitada[actual.x, actual.y + 1]) vecinos.Add(new Vector2Int(0, 1));

            if (vecinos.Count == 0) { pila.Pop(); continue; }

            Vector2Int dir = vecinos[rng.Next(vecinos.Count)];
            Vector2Int siguiente = actual + dir;

            if (dir.x == 1) paredEste[actual.x, actual.y] = false;
            else if (dir.x == -1) paredEste[siguiente.x, siguiente.y] = false;
            else if (dir.y == 1) paredSur[actual.x, actual.y] = false;
            else paredSur[siguiente.x, siguiente.y] = false;

            visitada[siguiente.x, siguiente.y] = true;
            pila.Push(siguiente);
        }
    }

    // ------------------------------------------------------------------
    // 2. Pasar de celdas logicas a grilla de tiles
    // ------------------------------------------------------------------
    private void ConstruirGrillaDeTiles()
    {
        int paso = anchoPasillo + grosorMuro;
        anchoTiles = celdasAncho * paso + grosorMuro;
        altoTiles = celdasAlto * paso + grosorMuro;

        esPiso = new bool[anchoTiles, altoTiles];

        for (int cx = 0; cx < celdasAncho; cx++)
        {
            for (int cy = 0; cy < celdasAlto; cy++)
            {
                int ox = grosorMuro + cx * paso;
                int oy = grosorMuro + cy * paso;

                Excavar(ox, oy, anchoPasillo, anchoPasillo);

                if (!paredEste[cx, cy] && cx < celdasAncho - 1)
                    Excavar(ox + anchoPasillo, oy, grosorMuro, anchoPasillo);

                if (!paredSur[cx, cy] && cy < celdasAlto - 1)
                    Excavar(ox, oy + anchoPasillo, anchoPasillo, grosorMuro);
            }
        }
    }

    private void Excavar(int x, int y, int ancho, int alto)
    {
        for (int a = 0; a < ancho; a++)
            for (int b = 0; b < alto; b++)
                if (x + a < anchoTiles && y + b < altoTiles)
                    esPiso[x + a, y + b] = true;
    }

    private void CalcularOrigen()
    {
        if (!centrarEnEsteObjeto) { origenReal = origen; return; }

        Vector3Int centro = suelo.WorldToCell(transform.position);
        origenReal = new Vector2Int(centro.x - anchoTiles / 2, centro.y - altoTiles / 2);
    }

    // ------------------------------------------------------------------
    // 3. Pintar los Tilemaps
    // ------------------------------------------------------------------
    private void Pintar()
    {
        suelo.ClearAllTiles();
        paredes.ClearAllTiles();

        var limites = new BoundsInt(origenReal.x, origenReal.y, 0, anchoTiles, altoTiles, 1);
        var buferSuelo = new TileBase[anchoTiles * altoTiles];
        var buferParedes = new TileBase[anchoTiles * altoTiles];

        for (int gy = 0; gy < altoTiles; gy++)
        {
            for (int gx = 0; gx < anchoTiles; gx++)
            {
                // La grilla tiene y hacia abajo y el Tilemap hacia arriba.
                int ty = altoTiles - 1 - gy;
                int i = ty * anchoTiles + gx;

                buferSuelo[i] = PisoAleatorio();

                if (!esPiso[gx, gy])
                    buferParedes[i] = MuroSegunVecinos(gx, gy);
            }
        }

        suelo.SetTilesBlock(limites, buferSuelo);
        paredes.SetTilesBlock(limites, buferParedes);

        ActualizarColisiones();
    }

    /// <summary>
    /// Fuerza a los colliders a rehacerse con los tiles nuevos.
    ///
    /// Sin esto pasa algo muy confuso: SetTilesBlock cambia lo que se DIBUJA,
    /// pero el CompositeCollider2D se queda con la geometria del laberinto
    /// anterior. Entonces caminas encima de paredes que ves y te frenas contra
    /// paredes invisibles en medio de un pasillo. El dibujo dice una cosa y la
    /// fisica otra.
    /// </summary>
    private void ActualizarColisiones()
    {
        var colisionadorTiles = paredes.GetComponent<TilemapCollider2D>();
        if (colisionadorTiles != null) colisionadorTiles.ProcessTilemapChanges();

        var compuesto = paredes.GetComponent<CompositeCollider2D>();
        if (compuesto != null)
        {
            if (compuesto.geometryType != CompositeCollider2D.GeometryType.Polygons)
            {
                compuesto.geometryType = CompositeCollider2D.GeometryType.Polygons;
                Debug.Log("[GeneradorLaberinto] Cambie el CompositeCollider2D a Polygons. " +
                          "Con Outlines los muros sólidos no colisionan bien.", this);
            }
            compuesto.GenerateGeometry();
        }

        // Deja las posiciones de fisica al dia antes de mover al jugador.
        Physics2D.SyncTransforms();
    }

    private bool EsPiso(int gx, int gy)
    {
        if (gx < 0 || gy < 0 || gx >= anchoTiles || gy >= altoTiles) return false;
        return esPiso[gx, gy];
    }

    /// <summary>
    /// Elige la pieza de muro mirando hacia donde queda el piso.
    /// El orden importa: primero las esquinas concavas (dos lados con piso),
    /// luego los bordes rectos (un lado), luego las convexas (solo diagonal).
    /// </summary>
    private TileBase MuroSegunVecinos(int gx, int gy)
    {
        // Recuerda: gy crece hacia ABAJO, asi que el norte del mundo es gy-1.
        bool n = EsPiso(gx, gy - 1);
        bool s = EsPiso(gx, gy + 1);
        bool e = EsPiso(gx + 1, gy);
        bool o = EsPiso(gx - 1, gy);

        if (n && e) return tileConcavaNE;
        if (n && o) return tileConcavaNO;
        if (s && e) return tileConcavaSE;
        if (s && o) return tileConcavaSO;

        if (s) return Elegir(tilesBordeArriba);
        if (n) return Elegir(tilesBordeAbajo);
        if (e) return Elegir(tilesBordeIzquierda);
        if (o) return Elegir(tilesBordeDerecha);

        if (EsPiso(gx + 1, gy + 1)) return tileConvexaSE;
        if (EsPiso(gx - 1, gy + 1)) return tileConvexaSO;
        if (EsPiso(gx + 1, gy - 1)) return tileConvexaNE;
        if (EsPiso(gx - 1, gy - 1)) return tileConvexaNO;

        return Elegir(tilesMuroInterior);
    }

    private TileBase Elegir(TileBase[] opciones)
    {
        if (opciones == null || opciones.Length == 0) return null;
        return opciones[rng.Next(opciones.Length)];
    }

    private TileBase PisoAleatorio()
    {
        bool hayDecorados = tilesPisoDecorado != null && tilesPisoDecorado.Length > 0;
        if (hayDecorados && rng.NextDouble() < probabilidadDecoracion)
            return tilesPisoDecorado[rng.Next(tilesPisoDecorado.Length)];
        return tilePiso;
    }

    // ------------------------------------------------------------------
    // 4. Entrada, salida y celdas libres
    // ------------------------------------------------------------------
    private void CalcularPuntosDeInteres()
    {
        CeldasCaminables.Clear();

        for (int gy = 0; gy < altoTiles; gy++)
            for (int gx = 0; gx < anchoTiles; gx++)
                if (esPiso[gx, gy])
                    CeldasCaminables.Add(MundoDesdeGrilla(gx, gy));

        int paso = anchoPasillo + grosorMuro;
        PosicionInicio = MundoDesdeGrilla(grosorMuro, grosorMuro);
        PosicionMeta = MundoDesdeGrilla(grosorMuro + (celdasAncho - 1) * paso,
                                        grosorMuro + (celdasAlto - 1) * paso);

        Vector3 min = suelo.CellToWorld(new Vector3Int(origenReal.x, origenReal.y, 0));
        Vector3 max = suelo.CellToWorld(new Vector3Int(origenReal.x + anchoTiles,
                                                       origenReal.y + altoTiles, 0));
        var limites = new Bounds();
        limites.SetMinMax(new Vector3(min.x, min.y, 0f), new Vector3(max.x, max.y, 0f));
        LimitesMundo = limites;
    }

    private Vector3 MundoDesdeGrilla(int gx, int gy)
    {
        int ty = altoTiles - 1 - gy;
        return suelo.GetCellCenterWorld(new Vector3Int(origenReal.x + gx, origenReal.y + ty, 0));
    }

    /// <summary>
    /// Devuelve una posicion caminable al azar, a por lo menos distanciaMinima
    /// de la entrada. Para no spawnear recolectables encima del jugador.
    /// </summary>
    public Vector3 PosicionLibreAleatoria(float distanciaMinima = 3f)
    {
        if (CeldasCaminables.Count == 0) return PosicionInicio;

        for (int intento = 0; intento < 40; intento++)
        {
            Vector3 p = CeldasCaminables[Random.Range(0, CeldasCaminables.Count)];
            if (Vector3.Distance(p, PosicionInicio) >= distanciaMinima) return p;
        }
        return CeldasCaminables[Random.Range(0, CeldasCaminables.Count)];
    }

#if UNITY_EDITOR
    /// <summary>
    /// Rellena todos los campos de tiles buscando los assets por nombre.
    /// Los numeros salen del slicing de Dungeon_walls.png: Unity corto de
    /// arriba hacia abajo y de izquierda a derecha saltandose las celdas
    /// vacias, por eso no son consecutivos de forma obvia.
    ///
    /// Corresponden (columna x fila de la hoja) a:
    ///   piso liso        c2f4
    ///   decorados        filas 1 y 2 completas
    ///   borde arriba     c2f3 + c3f8..c8f8
    ///   borde abajo      c2f5 + c3f9..c8f9
    ///   borde izquierda  c1f4 + c9f8, c9f9, c11f8
    ///   borde derecha    c3f4 + c10f8, c10f9, c12f8
    ///   concavas         c7f4 c6f4 c7f5 c6f5
    ///   convexas         c1f3 c3f3 c1f5 c3f5
    ///   muro interior    c1f6..c3f6
    ///
    /// Solo existe en el editor, pero los valores quedan guardados en la
    /// escena, asi que en el build siguen ahi.
    /// </summary>
    [ContextMenu("Autocompletar tiles")]
    private void AutocompletarTiles()
    {
        tilePiso = BuscarTile(33);

        tilesPisoDecorado = new TileBase[22];
        for (int i = 0; i <= 21; i++) tilesPisoDecorado[i] = BuscarTile(i);

        tilesBordeArriba = Buscar(23, 77, 78, 79, 80, 81, 82);
        tilesBordeAbajo = Buscar(43, 89, 90, 91, 92, 93, 94);
        tilesBordeIzquierda = Buscar(32, 83, 95, 85);
        tilesBordeDerecha = Buscar(34, 84, 96, 86);

        tileConcavaNE = BuscarTile(38);
        tileConcavaNO = BuscarTile(37);
        tileConcavaSE = BuscarTile(48);
        tileConcavaSO = BuscarTile(47);

        tileConvexaSE = BuscarTile(22);
        tileConvexaSO = BuscarTile(24);
        tileConvexaNE = BuscarTile(42);
        tileConvexaNO = BuscarTile(44);

        tilesMuroInterior = Buscar(53, 54, 55);

        int faltantes = 0;
        if (tilePiso == null) faltantes++;
        foreach (var t in tilesPisoDecorado) if (t == null) faltantes++;
        foreach (var t in tilesBordeArriba) if (t == null) faltantes++;
        foreach (var t in tilesBordeAbajo) if (t == null) faltantes++;
        foreach (var t in tilesBordeIzquierda) if (t == null) faltantes++;
        foreach (var t in tilesBordeDerecha) if (t == null) faltantes++;
        foreach (var t in tilesMuroInterior) if (t == null) faltantes++;

        if (faltantes > 0)
            Debug.LogWarning($"[GeneradorLaberinto] No encontre {faltantes} tiles. " +
                             "Revisa que los Dungeon_walls_*.asset sigan en el proyecto.", this);
        else
            Debug.Log("[GeneradorLaberinto] Tiles asignados correctamente.", this);

        UnityEditor.EditorUtility.SetDirty(this);
    }

    private static TileBase[] Buscar(params int[] indices)
    {
        var r = new TileBase[indices.Length];
        for (int i = 0; i < indices.Length; i++) r[i] = BuscarTile(indices[i]);
        return r;
    }

    // Busca por nombre exacto en todo el proyecto, asi no importa en que
    // carpeta esten los assets ni si los mueves despues.
    private static TileBase BuscarTile(int indice)
    {
        string nombre = "Dungeon_walls_" + indice;

        foreach (string guid in UnityEditor.AssetDatabase.FindAssets(nombre + " t:TileBase"))
        {
            string ruta = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(ruta) == nombre)
                return UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>(ruta);
        }
        return null;
    }
#endif

    // Dibuja el rectangulo del laberinto en la vista Scene.
    private void OnDrawGizmosSelected()
    {
        if (suelo == null) return;

        int paso = Mathf.Max(1, anchoPasillo) + Mathf.Max(1, grosorMuro);
        int w = Mathf.Max(2, celdasAncho) * paso + Mathf.Max(1, grosorMuro);
        int h = Mathf.Max(2, celdasAlto) * paso + Mathf.Max(1, grosorMuro);

        Vector2Int o = centrarEnEsteObjeto
            ? new Vector2Int(suelo.WorldToCell(transform.position).x - w / 2,
                             suelo.WorldToCell(transform.position).y - h / 2)
            : origen;

        Vector3 a = suelo.CellToWorld(new Vector3Int(o.x, o.y, 0));
        Vector3 b = suelo.CellToWorld(new Vector3Int(o.x + w, o.y + h, 0));

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube((a + b) * 0.5f, b - a);
    }
}
