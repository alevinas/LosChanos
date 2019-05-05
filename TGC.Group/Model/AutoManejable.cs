using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;

namespace TGC.Group.Model
{
    public class AutoManejable
    {
        private TgcMesh maya;
        public TgcMesh Maya { get => maya; set => maya = value; }
        public AutoManejable(TgcMesh valor)
        {
            maya = valor;
        }
        public float gradosGiro = FastMath.ToRad(1);
        public float velocidadMinima = -2;
        public float velocidadMaxima = 13;
        public float altura;
        public float alturaMaxima = 12;
        private int DireccionSalto { get; set; }
        public float velocidadSalto = 3.5f;
        private float direccionInicial;
        public float DireccionInicial { get => FastMath.ToRad(270); set => direccionInicial = value; }
        private float Aceleracion { get; set; }
        public bool VelocidadesCriticas { get => Velocidad < 0.05f && Velocidad > -0.05f; }
        public int Direccion { get; set; }
        public float Grados { get; set; }
        public float VelocidadInicial { get; set; }
        private float velocidad;
        public float Velocidad
        {
            get => FastMath.Min(FastMath.Max(VelocidadInicial + Aceleracion * Direccion, velocidadMinima), velocidadMaxima);
            set => velocidad = value;
        }

        public TGCVector3 VersorDirector()
        {
            return new TGCVector3(FastMath.Cos(DireccionInicial + Grados), 0, FastMath.Sin(DireccionInicial + Grados));
        }

        public float GiroTotal()
        {
            return gradosGiro * (Velocidad / 10);
        }

        //Movimiento
        public void Acelera()
        {
            if (Velocidad >= 0)
            {
                Aceleracion += 0.02f;
                Direccion = 1;
            }
        }
        public void Frena()
        {

            if (VelocidadesCriticas)
            {
                this.Parado();
            }
            else
            {
                Aceleracion -= 0.1f;
            }
        }
        public void MarchaAtras()
        {
            if (Velocidad <= 0)
            {
                Aceleracion += 0.02f;
                Direccion = -1;
            }
            else
            {
                this.Parado();
            }
        }
        public void GiraDerecha()
        {
            Grados -= GiroTotal();
        }
        public void GiraIzquierda()
        {
            Grados += GiroTotal();     
        }
        public void Parado()
        {
            VelocidadInicial = Velocidad;
            Aceleracion = 0;
            if (Velocidad != 0)
            {
                if (VelocidadesCriticas)
                {
                    VelocidadInicial = 0;
                }
                else
                {
                    Aceleracion -= 0.008f;
                }
            }
        }

        public void Salta()
        {
            altura += 30;
            if (Maya.Position.Y > alturaMaxima)
            {
                DireccionSalto = -1;
            }
            else if (Maya.Position.Y < 0)
            {
                DireccionSalto = 1;
                altura = 0;
            }
            Maya.Position += new TGCVector3(0, velocidadSalto * DireccionSalto * altura, 0);
        }


        /*  
            El bug del salto del auto es que doblando hacia derecha y acelerando no es posible saltar porque se solapan los ejes. El rotateY(-giroTotal) esta restando en el eje Y
            y me impide saltar. Creo que hay que usar TGCVector3.TransformCoordinate()
         */
        public float ElapsedTime { get => elapsedTime; set => elapsedTime = value; }

        private float elapsedTime;
        public TGCMatrix Traslacion { get => TGCMatrix.Translation(VersorDirector().X * Velocidad, 0, VersorDirector().Z * Velocidad); }
        public TGCMatrix Rotacion {  get => TGCMatrix.RotationY(-Grados); }
        public TGCMatrix Movimiento { get => Rotacion * TraslacionAcumulada; }
        public TGCMatrix TraslacionAcumulada = TGCMatrix.Identity;
        public void Moverse()
        {
            TraslacionAcumulada *= Traslacion;
            Maya.Position += (VersorDirector() * Velocidad);
            Maya.Transform = Movimiento;
         
        }
    }
}


