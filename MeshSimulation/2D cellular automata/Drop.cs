using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshSimulation
{
    public struct Interface
    {
        public List<Cell> LiquidCellList;
        public List<Cell> GasCellList;
        public Cell LiquidCell;
        public Cell GasCell;
        public int BubbleIndex;
        public int DropIndex;

        public Interface(Cell liquidCell, Cell gasCell)
        {
            LiquidCellList = new List<Cell>();
            GasCellList = new List<Cell>();

            LiquidCell = new Cell(0, 0);
            GasCell = new Cell(0, 0);

            LiquidCell = liquidCell;
            GasCell = gasCell;
            BubbleIndex = gasCell.index;
            DropIndex = liquidCell.index;
        }
    }
    public class Drop
    {
        public List<Cell> CellList;
        public double Pressure;
        public List<Interface> InterfaceList;
        public bool isSource;

        public Drop(List<Cell> cellList)
        {
            CellList = new List<Cell>();
            CellList = cellList;
            Pressure = 0;
            InterfaceList = new List<Interface>();
            isSource = false;
        }
        public Drop(List<Cell> cellList, double pressure)
        {
            CellList = new List<Cell>();
            CellList = cellList;
            Pressure = pressure;
            InterfaceList = new List<Interface>();
            isSource = true;
        }
    }
}
