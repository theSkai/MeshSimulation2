using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshSimulation
{
    public struct Interface3D
    {
        public Cell3D LiquidCell;
        public Cell3D GasCell;
        public int BubbleIndex;
        public int DropIndex;

        public Interface3D(Cell3D liquidCell, Cell3D gasCell)
        {
            LiquidCell = new Cell3D(0, 0, 0);
            GasCell = new Cell3D(0, 0, 0);

            LiquidCell = liquidCell;
            GasCell = gasCell;

            DropIndex = liquidCell.Index;
            BubbleIndex = gasCell.Index;

        }
    }
    public class Drop3D
    {
        public List<Cell3D> CellList;
        public double Pressure;
        public List<Interface3D> InterfaceList;
        public bool isSource;

        public Drop3D(List<Cell3D> cellList)
        {
            CellList = new List<Cell3D>();
            CellList = cellList;
            Pressure = 0;
            InterfaceList = new List<Interface3D>();
            isSource = false;
        }
        public Drop3D(List<Cell3D> cellList, double sourcePressure)
        {
            CellList = new List<Cell3D>();
            CellList = cellList;
            Pressure = sourcePressure;
            InterfaceList = new List<Interface3D>();
            isSource = true;
        }
    }
}
