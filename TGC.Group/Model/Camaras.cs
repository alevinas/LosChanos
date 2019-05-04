
using TGC.Core.Camara;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;

namespace TGC.Group.Model
{

    public class CamaraAtras : TgcCamera
    {
        public AutoManejable objetivo;
        public CamaraAtras(AutoManejable nuevo_objetivo)
        {
            objetivo = nuevo_objetivo;
            this.SetCamera(PosicionCamaraAtras, objetivo.Maya.Position);
        }
        public float distanciaCamaraAtras = 200;
        public float alturaCamaraAtras = 50;
        private float lambda;
        public float Lambda { get => distanciaCamaraAtras / FastMath.Sqrt((FastMath.Pow2(objetivo.VersorDirector().X)) + FastMath.Pow2(objetivo.VersorDirector().Z)); set => lambda = value; }
        private TGCVector3 posicionCamaraAtras;
        public TGCVector3 PosicionCamaraAtras { get => new TGCVector3(objetivo.Maya.Position.X - (Lambda * objetivo.Direccion * objetivo.VersorDirector().X), alturaCamaraAtras, objetivo.Maya.Position.Z - (Lambda * objetivo.Direccion * objetivo.VersorDirector().Z)); set => posicionCamaraAtras = value; }
    }
    public class CamaraAerea : TgcCamera {

         public TgcMesh objetivo;
         TGCVector3 posicionCamaraArea = new TGCVector3(50, 2900, 0);
         public CamaraAerea(TgcMesh nuev_objetivo)
         {
             objetivo = nuev_objetivo;
             this.SetCamera(posicionCamaraArea, objetivo.Position);
         }

    }
    public class CamaraFija : TgcCamera {

         public CamaraFija()
         {
             this.SetCamera(new TGCVector3(50, 2900, 0), TGCVector3.Empty);
         }
    }

}