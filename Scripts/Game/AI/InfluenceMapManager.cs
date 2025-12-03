using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
// para el grid
using Components;

namespace Game;

public partial class InfluenceMapManager : Node2D
{
	// MAPITAS DE INFLUENCIA
	public float[,] TroopInfluence { get; private set; } //Tropas
	public float[,] MoveCostMap { get; private set; } //Coste de movimiento
	public float[,] StructureValueMap { get; private set; } //Estructuras a conquistar

	private int width;
	private int height;
	private BoardState boardState;
	private Grid2D grid;

	// RADIO PARA LA FORMULITA
	public int MaxInfluenceRadius { get; set; } = 4;

	// FILTRO DE CONVOLUCION -> guardado por si quieres probarlo, pero no lo usamos aquí
	private readonly float[,] kernel3x3 =
	{
		{ 0.25f, 0.50f, 0.25f },
		{ 0.50f, 1.00f, 0.50f },
		{ 0.25f, 0.50f, 0.25f }
	};

	public override void _Ready()
	{
		boardState = Board.State;
		grid = Board.Grid;
	}

	// La llamará BoardState cuando haya acabado de crear el tablero
	public void Initialize(BoardState state)
	{
		boardState = state;

		width  = grid.Size.X;
		height = grid.Size.Y;

		TroopInfluence     = new float[width, height];
		MoveCostMap        = new float[width, height];
		StructureValueMap  = new float[width, height];

		boardState.MinionMoved += OnMinionMoved;
		boardState.MinionAdded += OnMinionAdded;
		boardState.MinionDeath += OnMinionDeath;

		RebuildTroopInfluence();
		RebuildMoveCostMap();
		RebuildStructureValueMap();

	}

	// =========================
	// MAPA DE INFLUENCIA TROPAS
	// =========================
	public void RebuildTroopInfluence()
	{
		// Limpia el mapa
		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
			TroopInfluence[x, y] = 0f;

		foreach (var minion in boardState.Minions)
		{
			Vector2I pos = minion.Position;
			if (!IsInside(pos))
				continue;

			// Signo según bando (+ aliado, - enemigo)
			float sign = (minion.Owner == Board.Players.Player1) ? 1f : -1f;

			// PONERLO POR TIPO DE TROPAS
			float strength = 1f; //Ahora valen todas lo mismo
			float baseValue = sign * strength;

			// Ahora las influencias por decaimiento lineal usando la distancia Manhattan
			for (int dy = -MaxInfluenceRadius; dy <= MaxInfluenceRadius; dy++)
			for (int dx = -MaxInfluenceRadius; dx <= MaxInfluenceRadius; dx++)
			{
				Vector2I cell = new(pos.X + dx, pos.Y + dy);
				if (!IsInside(cell))
					continue;

				int dist = Math.Abs(dx) + Math.Abs(dy);
				if (dist > MaxInfluenceRadius)
					continue;
					
				//LA FORMULITA
				float factor = 1f - (dist / (float)MaxInfluenceRadius);
				float influence = baseValue * factor;

				TroopInfluence[cell.X, cell.Y] += influence;
			}
		}

		GD.Print($"La casillita at (3,3): {TroopInfluence[3,3]}");
	}

	// =========================
	// MAPA DE COSTE DE MOVIMIENTO
	// =========================
	public void RebuildMoveCostMap()
	{
		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
		{
			var cell = new Vector2I(x, y);
			var tile = boardState.Tiles.GetValueOrDefault(cell, null);
			MoveCostMap[x, y] = (tile != null) ? tile.MoveCost : float.PositiveInfinity;
		}
	}

	// =========================
	// MAPA DE ESTRUCTURAS
	// =========================
	public void RebuildStructureValueMap()
	{
		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
			StructureValueMap[x, y] = 0f;

		foreach (var fort in boardState.Forts)
		{
			Vector2I pos = fort.Position;
			if (!IsInside(pos))
				continue;

			StructureValueMap[pos.X, pos.Y] = 1f;
		}
	}

	// =========================
	// EVENTOS
	// =========================
	private void OnMinionMoved(Minion minion, Vector2I[] path)
	{
		GD.Print($"¡Evento MinionMoved recibido! Minion: {minion}, path length: {path.Length}");
		RebuildTroopInfluence();
	}

	private void OnMinionAdded(Minion minion)
	{
		GD.Print($"¡Minion añadido: {minion.Name}");
		RebuildTroopInfluence();
		
		//PRUEBAS
		//var zonas = FindNoMansLandCells();
		//GD.Print("Zonas de nadie: ", zonas.Count);
	}

	private void OnMinionDeath(Minion minion)
	{
		GD.Print($"¡Minion muerto: {minion.Name}");
		RebuildTroopInfluence();
	}

	// =========================
	// MÉTODOS DE CONSULTA
	// =========================
	public float GetInfluenceAt(Vector2I cell)
	{
		if (!IsInside(cell))
			return 0f;

		return TroopInfluence[cell.X, cell.Y];
	}

	/// PUNTO DÉBIL FRONTERA
	public Vector2I? FindWeakAllyFrontierCell(float frontierThreshold = 0.5f, float strongAllyThreshold = 1.0f)
	{
		Vector2I? bestCell = null;
		float bestValue = float.PositiveInfinity;

		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
		{
			Vector2I cell = new(x, y);
			float totalInfluence = TroopInfluence[x, y];
			float allyInfluence = Math.Max(0f, totalInfluence);

			if (allyInfluence > frontierThreshold)
				continue;

			if (!HasStrongAllyNeighbor(cell, strongAllyThreshold))
				continue;

			var cellData = boardState.GetCellData(cell);
			if (cellData.Tile == null)
				continue;

			if (allyInfluence < bestValue)
			{
				bestValue = allyInfluence;
				bestCell = cell;
			}
		}

		return bestCell;
	}

	/// ZONA DE NADIE
	public List<Vector2I> FindNoMansLandCells(float threshold = 0.1f)
	{
		var list = new List<Vector2I>();

		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
		{
			float total = TroopInfluence[x, y];
			float ally  = Math.Max(0f,  total);
			float enemy = Math.Max(0f, -total);

			if (ally < threshold && enemy < threshold)
				list.Add(new Vector2I(x, y));
		}

		return list;
	}

	// =========================
	// MÉTODOS AUXILIARES
	// =========================
	private bool IsInside(Vector2I cell)
	{
		return grid.IsInsideGrid(cell);
	}

	private bool HasStrongAllyNeighbor(Vector2I cell, float strongAllyThreshold)
	{
		foreach (var n in grid.GetAdjacents(cell, includeDiagonals: true))
		{
			float totalInfluence = TroopInfluence[n.X, n.Y];
			float allyInfluence  = Math.Max(0f, totalInfluence);
			if (allyInfluence >= strongAllyThreshold)
				return true;
		}
		return false;
	}
	

	// AHORA NO SE USA CONVOLUCIÓN POR FILTRO
	private void Convolve(float[,] baseMap, float[,] kernel, float[,] outMap)
	{
		int kWidth  = kernel.GetLength(0);
		int kHeight = kernel.GetLength(1);
		int kHalfW  = kWidth / 2;
		int kHalfH  = kHeight / 2;

		Array.Clear(outMap, 0, outMap.Length);

		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
		{
			float sum = 0f;

			for (int ky = 0; ky < kHeight; ky++)
			for (int kx = 0; kx < kWidth; kx++)
			{
				int sx = x + kx - kHalfW;
				int sy = y + ky - kHalfH;

				if (sx < 0 || sy < 0 || sx >= width || sy >= height)
					continue;

				float w = kernel[kx, ky];
				sum += baseMap[sx, sy] * w;
			}

			outMap[x, y] = sum;
		}
	}
	
	// =========================
	// MEJOR CASILLA, EN BASE A UN FILTRO Y UNA PUNTUACIÓN
	// =========================
	
	//Ejemplo aplicacion metodo
	//La IA busca la mejor casilla para avanzar y presionar al jugador
	//Filtro: que la casilla sea transitable
	//Puntuacion: en funcion de los valores que haya en los mapas u otras cosas
	
	
	public Vector2I? FindBestCell( 
		Func<Vector2I, bool> filter, //Definir una funcion a modo de filtro
		Func<Vector2I, float> score) //Funcion para evaluar la mejor casilla de todas
	{
		Vector2I? bestCell = null;
		float bestValue = float.NegativeInfinity;

		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
		{
			var cell = new Vector2I(x, y);
			if (!IsInside(cell))
				continue;
			if (!filter(cell))
				continue;

			float value = score(cell);
			if (value > bestValue)
			{
				bestValue = value;
				bestCell = cell;
			}
		}

		return bestCell;
	}
	
	
	
	
	
}
