using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

//para el grid
using Components;


namespace Game;


public partial class InfluenceMapManager : Node2D
{
	// MAPA DE INFLUENCIA DE LAS TROPAS
	public float[,] TroopInfluence { get; private set; }
	
	//ANADIR MAPA DE INFLUENCIA DEL TERRENO Y LAS ESTRUCTURAS

	private int width;
	private int height;
	private BoardState boardState;
	private Grid2D grid;

	// FILTRO DE CONVOLUCION --> MODIFICARLO SI ES NECESARIO
	private readonly float[,] kernel3x3 =
	{
		{ 0.25f, 0.50f, 0.25f },
		{ 0.50f, 1.00f, 0.50f },
		{ 0.25f, 0.50f, 0.25f }
	};

	public override void _Ready()
	{
		//Aweno
		boardState = Board.State;
		grid = Board.Grid;
	}
	

	// La llamará BoardState cuando haya acabado de crear el tablero
	public void Initialize(BoardState state)
	{
		boardState = state;

		width  = grid.Size.X;
		height = grid.Size.Y;

		TroopInfluence = new float[width, height];

		boardState.MinionMoved += OnMinionMoved;
		boardState.MinionAdded += OnMinionAdded;
		boardState.MinionDeath += OnMinionDeath;

		RebuildTroopInfluence();
		// GD.Print("InfluenceMapManager inicializado. Minions actuales: ", boardState.Minions.Count);
	}

	public void RebuildTroopInfluence()
	{
		float[,] baseMap = new float[width, height];

		foreach (var minion in boardState.Minions)
		{
			Vector2I pos = minion.Position;
			if (!IsInside(pos))
				continue;

			//HE REALIZADO CALCULOS DE LA SIGUIENTE MANERA
			// TROPAS ALIADAS --> TODAS A +1
			// TROPAS ENEMIGAS --> TODAS A -1
			//SE CAMBIA Y AU
			float value = (minion.Owner == Board.Players.Player1) ? 1f : -1f;
			baseMap[pos.X, pos.Y] += value;
		}

		Convolve(baseMap, kernel3x3, TroopInfluence);
		
		//PRUEBA CODIGO
		// GD.Print($"La casillita at (3,3): {TroopInfluence[3,3]}");

	}
	
	
	
	
	
	//Eventos para actualizar el mapita

	private void OnMinionMoved(Minion minion, Vector2I[] path)
	{
		//GD.Print($"¡Evento MinionMoved recibido! Minion: {minion}, path length: {path.Length}");
		RebuildTroopInfluence();
	}


	private void OnMinionAdded(Minion minion)
	{
		//GD.Print($"¡Minion añadido: {minion.Name}");
		RebuildTroopInfluence();
	}

	private void OnMinionDeath(Minion minion)
	{
		//GD.Print($"¡Minion muerto: {minion.Name}");
		RebuildTroopInfluence();
	}




	public float GetInfluenceAt(Vector2I cell)
	{
		if (!IsInside(cell))
			return 0f;

		return TroopInfluence[cell.X, cell.Y];
	}
	
	

	/// METODO PARA CALCULAR PUNTO DEBIL FRONTERA (ESTE ALONSO)
	public Vector2I? FindWeakAllyFrontierCell(float frontierThreshold = 0.5f, float strongAllyThreshold = 1.0f)
	{
		Vector2I? bestCell = null;
		float bestValue = float.PositiveInfinity; //Solamente calcularemos para las tropas aliadas

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				Vector2I cell = new(x, y);
				float totalInfluence = TroopInfluence[x, y];

				float allyInfluence = Math.Max(0f, totalInfluence);

				// Casilla con influencia aliada bajita
				if (allyInfluence > frontierThreshold)
					continue;

				// Mirar si el vecino es influencia enemiga
				if (!HasStrongAllyNeighbor(cell, strongAllyThreshold))
					continue;

				// Por si las moscas
				var cellData = boardState.GetCellData(cell);
				if (cellData.Tile == null)
					continue;

				// ANADIR FILTRO POR COSTE DE MOVIMIENTO U OTROS PARA LOS OTROS MAPITAS

				if (allyInfluence < bestValue)
				{
					bestValue = allyInfluence;
					bestCell = cell;
				}
			}
		}

		return bestCell;
	}


	//METODOS AUXILIARES

private bool IsInside(Vector2I cell)
{
	return grid.IsInsideGrid(cell);
}


private bool HasStrongAllyNeighbor(Vector2I cell, float strongAllyThreshold)
{
	//No me acordaba de este metodo
	foreach (var n in grid.GetAdjacents(cell, includeDiagonals: true))
	{
		float totalInfluence = TroopInfluence[n.X, n.Y];
		float allyInfluence = Math.Max(0f, totalInfluence);
		if (allyInfluence >= strongAllyThreshold)
			return true;
	}
	return false;
}


	//Que pereza de metodo
	private void Convolve(float[,] baseMap, float[,] kernel, float[,] outMap)
	{
		int kWidth  = kernel.GetLength(0);
		int kHeight = kernel.GetLength(1);
		int kHalfW  = kWidth / 2;
		int kHalfH  = kHeight / 2;

		Array.Clear(outMap, 0, outMap.Length);
		
		//Eficiencia al maximo con 4 bucles anidados (consultar si hay otra forma)

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				float sum = 0f;

				for (int ky = 0; ky < kHeight; ky++)
				{
					for (int kx = 0; kx < kWidth; kx++)
					{
						int sx = x + kx - kHalfW;
						int sy = y + ky - kHalfH;

						if (sx < 0 || sy < 0 || sx >= width || sy >= height)
							continue;

						float w = kernel[kx, ky];
						sum += baseMap[sx, sy] * w;
					}
				}

				outMap[x, y] = sum;
			}
		}
	}

}
