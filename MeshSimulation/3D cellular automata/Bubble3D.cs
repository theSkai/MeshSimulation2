using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshSimulation
{
    public class Bubble3D
    {
        public List<Cell3D> CellList;
        public double BubbleGasAmount;
        public double BubbleVolume;
        public double Pressure;

        public Bubble3D(List<Cell3D> cellList)
        {
            CellList = new List<Cell3D>();
            CellList = cellList;

            BubbleGasAmount = 0;
            BubbleVolume = 0;
            Pressure = 0;
        }
    }
}
