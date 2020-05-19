using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Geometry;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace MeshSimulation
{
    public class ghc_DisplayMesh : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ghc_DisplayMesh class.
        /// </summary>
        public ghc_DisplayMesh()
          : base("ghc_DisplayMesh", "DisplayMesh",
              "DisplayMesh",
              "venous", "mesh")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Mesh", "Mesh", "Mesh", GH_ParamAccess.list);
            pManager.AddNumberParameter("Step", "Step", "Step", GH_ParamAccess.item, 1.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "mesh", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<List<int>> displayMesh = new List<List<int>>();
            double a = 1.0;

            bool sucess = DA.GetDataList(0, displayMesh);
            DA.GetData(1, ref a);

            if (sucess && displayMesh.Count>0)
            {
                int width = displayMesh.Count;
                int height = displayMesh[0].Count;
                List<GH_Mesh> mesh = new List<GH_Mesh>();
                for(int i = 0; i < width; i++)
                {
                    for(int j = 0; j < height; j++)
                    {
                        Interval xInt = new Interval(i * a, (i + 1) * a);
                        Interval yInt = new Interval(j * a, (j + 1) * a);
                        Mesh m = Mesh.CreateFromPlane(Rhino.Geometry.Plane.WorldXY, xInt, yInt, 1, 1);
                        if (displayMesh[i][j] == 0) m.VertexColors.CreateMonotoneMesh(Color.Gray);
                        else if (displayMesh[i][j] == 1) m.VertexColors.CreateMonotoneMesh(Color.White);
                        else if (displayMesh[i][j] == 2) m.VertexColors.CreateMonotoneMesh(Color.Blue);
                        else m.VertexColors.CreateMonotoneMesh(Color.Black);
                        mesh.Add(new GH_Mesh(m));
                    }
                }
                DA.SetDataList(0, mesh);
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
            get { return new Guid("31b05865-b33d-4774-bdbd-bad8939780b2"); }
        }
    }
}