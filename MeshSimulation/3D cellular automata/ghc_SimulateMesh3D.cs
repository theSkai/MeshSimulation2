using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace MeshSimulation
{
    public class ghc_SimulateMesh3D : GH_Component
    {
        public ghc_SimulateMesh3D()
          : base("MeshSimulation3D", "MeshSimulation3D",
              "simulate mesh fluid distribution",
              "venous", "mesh")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset", "Reset", "Reset", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Step", "Step", "Step", GH_ParamAccess.item, 0);

            pManager.AddGenericParameter("VesselCellList", "VesselCellList", "VesselCellList", GH_ParamAccess.list);
            pManager.AddPointParameter("LiquidSourcePointList", "LiquidSourcePoint", "LiquidSourcePoint", GH_ParamAccess.list);
            pManager.AddNumberParameter("LiquidSourcePressureList", "LiquidSourcePressure", "LiquidSourcePressure", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Output", "Output", "Output", GH_ParamAccess.list);
            pManager.AddGenericParameter("DisplayMesh", "DisplayMesh", "DisplayMesh", GH_ParamAccess.list);
        }

        bool isInitialized = false;
        public Canvas3D canvas = new Canvas3D();
        public Canvas3D canvas1 = new Canvas3D();
        public Canvas3D canvas2 = new Canvas3D();

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool Reset = true;
            double step = 0;
            int Step = 0;
            List<Cell3D> VesselCellList = new List<Cell3D>();//后续直接引用
            List<Point3d> LiquidSource = new List<Point3d>();
            List<double> LiquidSourcePressure = new List<double>();

            List<GH_Mesh> displayMesh = new List<GH_Mesh>();
            List<string> output = new List<string>();
            double SumGasAmount = 0;

            DA.GetData("Step", ref step);
            DA.GetData("Reset", ref Reset);
            DA.GetDataList("VesselCellList", VesselCellList);
            DA.GetDataList("LiquidSourcePointList", LiquidSource);
            DA.GetDataList("LiquidSourcePressureList", LiquidSourcePressure);

            Step = (int)step;
            if (step == 0) return;
            if (Reset)
            {
                isInitialized = true;
                canvas = new Canvas3D();
                canvas.Step = Step;
                canvas.InitializeVesselCellListFrom(VesselCellList, false);
                canvas.LoadLiquidSource(LiquidSource, LiquidSourcePressure);
                canvas1.InitializeVesselCellListFrom(canvas.VesselCellList, true);
                canvas2.InitializeVesselCellListFrom(canvas.VesselCellList, true);

                SumGasAmount = canvas.SumGasAmount;
                output.Clear();
                output.Add(SumGasAmount.ToString());
            }
            
            else if (isInitialized)
            {
                int updateTimes = 0;
                while (updateTimes < 100)
                {
                    canvas.UpdateLiquidSourcePressure(LiquidSourcePressure);
                    canvas.ReadMesh();
                    canvas.UpdateFluidDistribution();
                    SumGasAmount = canvas.SumGasAmount;

                    if (updateTimes >= 2)
                    {
                        if (canvas2.IsSameVesselCellListAs(canvas.VesselCellList))
                        {
                            break;
                        }
                    }

                    canvas2.UpdateVesselCellListFrom(canvas1.VesselCellList);
                    canvas1.UpdateVesselCellListFrom(canvas.VesselCellList);

                    updateTimes++;
                }
                output.Clear();
                output.Add("update times: " + updateTimes.ToString());
                output.Add("gas amount: " + SumGasAmount.ToString());
            }

            displayMesh = canvas.DisplayMeshColored();

            DA.SetDataList("DisplayMesh", displayMesh);
            DA.SetDataList("Output", output);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("0619c7e2-a76e-4a01-83d6-13e0b1a46b11"); }
        }
    }
}