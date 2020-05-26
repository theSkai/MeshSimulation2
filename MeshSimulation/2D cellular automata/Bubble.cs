using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshSimulation
{
    public class Bubble
    {
        public List<Cell> CellList;
        public double SumGasAmount;
        public int SumVolume;
        public double Pressure;

        public Bubble(List<Cell> cellList)
        {
            CellList = new List<Cell>();
            SumGasAmount = 0;
            SumVolume = 0;
            Pressure = 0;
            CellList = cellList;

        }
    }
}
