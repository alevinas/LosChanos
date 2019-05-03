using Microsoft.DirectX.DirectInput;
using System.Drawing;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Camara;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;

namespace TGC.Group.Model
{

		public class CamaraAtras : TgcCamera{
			public AutoManejable objetivo;
            public CamaraAtras(AutoManejable nuevo_objetivo)
                {
                    objetivo = nuevo_objetivo;
                    this.SetCamera(posicionCamaraAtras, objetivo.Maya.Position);
                }
            public float distanciaCamaraAtras = 200;
            public float alturaCamaraAtras = 50;
            private float lambda;
            public float Lambda { get => distanciaCamaraAtras / FastMath.Sqrt((FastMath.Pow2(objetivo.versorDirector().X)) + FastMath.Pow2(objetivo.versorDirector().Z)); set => lambda = value; }
            private TGCVector3 posicionCamaraAtras;
            public TGCVector3 PosicionCamaraAtras { get => new TGCVector3(objetivo.Maya.Position.X - (lambda * objetivo.Direccion * objetivo.versorDirector().X), alturaCamaraAtras, objetivo.Maya.Position.Z - (lambda * objetivo.Direccion * objetivo.versorDirector().Z)); set => posicionCamaraAtras = value; }


    }
    public class CamaraAerea : TgcCamera{
			
			public TgcMesh objetivo;
            TGCVector3 posicionCamaraArea = new TGCVector3(50, 2900, 0);
                public CamaraAerea(TgcMesh nuev_objetivo)
                {
                    objetivo = nuev_objetivo;
                    this.SetCamera(posicionCamaraArea, objetivo.Position);
                }
			
		}
		
}