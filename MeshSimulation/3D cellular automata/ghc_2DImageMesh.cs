using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace MeshSimulation._3D_cellular_automata
{
    public class ghc_2DImageMesh : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ghc_2DImageMesh class.
        /// </summary>
        public ghc_2DImageMesh()
          : base("2DImageMesh", "2DImageMesh",
              "2DImageMesh",
              "venous", "mesh")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddColourParameter("ColorList", "ColorList", "ColorList", GH_ParamAccess.list);
            pManager.AddNumberParameter("Width", "Width", "Width", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Height", "Height", "Height", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("VesselCellList", "VesselCellList", "VesselCellList", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double width = 0;
            double height = 0;
            DA.GetData("Width", ref width);
            DA.GetData("Height", ref height);
            int Width = (int)width;
            int Height = (int)height;

            List<Color> ColorList = new List<Color>();
            DA.GetDataList("ColorList", ColorList);

            CreateEmptyMesh(Width, Height, 1);
            InitializeMesh(Width, Height, 1, ColorList);

            DA.SetDataList("VesselCellList", VesselCellList);
        }

        public List<List<List<Cell3D>>> CellMesh = new List<List<List<Cell3D>>>();//x,y,z 
        public List<Cell3D> VesselCellList = new List<Cell3D>();

        public void CreateEmptyMesh(int X_extent, int Y_extent, int Z_extent)
        {
            CellMesh.Clear();//开新空间
            for (int i = 0; i < X_extent; i++)
            {
                List<List<Cell3D>> columnYZ = new List<List<Cell3D>>();
                for (int j = 0; j < Y_extent; j++)
                {
                    List<Cell3D> columnZ = new List<Cell3D>();
                    List<bool> columnOfIsDiscoveredZ = new List<bool>();
                    for (int k = 0; k < Z_extent; k++)
                    {
                        Cell3D c = new Cell3D(i, j, k);//物理坐标
                        columnZ.Add(c);
                        columnOfIsDiscoveredZ.Add(false);
                    }
                    columnYZ.Add(columnZ);
                }
                CellMesh.Add(columnYZ);
            }
        }

        private int ColorIndex(int i, int j, int X_extent, int Y_extent)
        {
            return (Y_extent - j - 1) * X_extent + i;
        }
        private bool IsGas(Color color)
        {
            if (color.R > 200 && color.G > 200 && color.B > 200) return true;
            else return false;
        }
        private bool IsLiquid(Color color)
        {
            if (color.R < 50 && color.G < 50 && color.B > 200) return true;
            else return false;
        }
        public void InitializeMesh(int X_extent, int Y_extent, int Z_extent, List<Color> ColorList)
        {
            VesselCellList.Clear();//开新空间
            for (int i = 0; i < X_extent; i++)
            {
                for (int j = 0; j < Y_extent; j++)
                {
                    for (int k = 0; k < Z_extent; k++)
                    {
                        int colorIndex = ColorIndex(i, j, X_extent, Y_extent);
                        Color currentColor = ColorList[colorIndex];
                        if(IsGas(currentColor) || IsLiquid(currentColor))
                        {
                            //定义空腔
                            CellMesh[i][j][k].Volume = 1;
                            if (IsGas(currentColor)) CellMesh[i][j][k].Phase = 1;
                            else CellMesh[i][j][k].Phase = 0;

                            if (i + 1 < X_extent)
                            {
                                Color neighbourColor = ColorList[ColorIndex(i + 1, j, X_extent, Y_extent)];
                                if (IsGas(neighbourColor) || IsLiquid(neighbourColor)) CellMesh[i][j][k].Neighbour.Add(CellMesh[i + 1][j][k]);
                            }
                            if (i - 1 >= 0)
                            {
                                Color neighbourColor = ColorList[ColorIndex(i - 1, j, X_extent, Y_extent)];
                                if (IsGas(neighbourColor) || IsLiquid(neighbourColor)) CellMesh[i][j][k].Neighbour.Add(CellMesh[i - 1][j][k]);
                            }
                            if (j + 1 < Y_extent)
                            {
                                Color neighbourColor = ColorList[ColorIndex(i, j + 1, X_extent, Y_extent)];
                                if (IsGas(neighbourColor) || IsLiquid(neighbourColor)) CellMesh[i][j][k].Neighbour.Add(CellMesh[i][j + 1][k]);
                            }
                            if (j - 1 >= 0)
                            {
                                Color neighbourColor = ColorList[ColorIndex(i, j - 1, X_extent, Y_extent)];
                                if (IsGas(neighbourColor) || IsLiquid(neighbourColor)) CellMesh[i][j][k].Neighbour.Add(CellMesh[i][j - 1][k]);
                            }
                            VesselCellList.Add(CellMesh[i][j][k]);//按照坐标序加入                        
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("c072b3a0-da7b-4ec0-a1ac-9fbdbfe4bca3"); }
        }
    }
}