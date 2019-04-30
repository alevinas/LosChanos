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
    /// <summary>
    ///     Ejemplo para implementar el TP.
    ///     Inicialmente puede ser renombrado o copiado para hacer más ejemplos chicos, en el caso de copiar para que se
    ///     ejecute el nuevo ejemplo deben cambiar el modelo que instancia GameForm <see cref="Form.GameForm.InitGraphics()" />
    ///     line 97.
    /// </summary>
    public class GameModel : TgcExample
    {
        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="shadersDir">Ruta donde esta la carpeta con los shaders</param>
        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;
        }

        //Objetos viejos
        /*private TgcMesh Piso { get; set; }     // Resta definir si Estadio o Ciudad
        private TgcMesh Pared { get; set; }
        private TgcMesh Tribuna { get; set; }
        private TGCBox Box { get; set; }
        */

        //Objetos nuevos
        private AutoManejable Automotor2 { get; set; }
        private TgcScene Ciudad { get; set; }

        public class CamaraAtrasRotadora : TgcCamera
        {
            private AutoManejable objetivo;
            public CamaraAtrasRotadora(AutoManejable automotor) {
                objetivo = automotor;
            }
            public float distanciaCamaraAtras = 200;
            public float alturaCamaraAtras = 50;
            private float lambda;
            public float Lambda { get => distanciaCamaraAtras / FastMath.Sqrt((FastMath.Pow2(objetivo.versorDirector().X)) + FastMath.Pow2(objetivo.versorDirector().Z)); set => lambda = value; }
            private TGCVector3 posicionCamaraAtras;
            public TGCVector3 PosicionCamaraAtras { get => new TGCVector3(objetivo.Position.X - (lambda * objetivo.Direccion * objetivo.versorDirector().X), alturaCamaraAtras, objetivo.Position.Z - (lambda * objetivo.Direccion * objetivo.versorDirector().Z)); set => posicionCamaraAtras = value; }

        }

        public class AutoManejable : TgcMesh
        {
            public float gradosGiro = 0.017f;
            public float velocidadMinima = -2;
            public float velocidadMaxima = 13;

            private float aceleracion = 0;
            public float Aceleracion { get => aceleracion; set => aceleracion = value; }
            private float tiempoBotonApretado = 0.0f;
            public float TiempoBotonApretado { get => tiempoBotonApretado; set => tiempoBotonApretado = value; }
            private int direccion = 1;
            public int Direccion { get => direccion; set => direccion = value; }
            private float rozamiento = 0.005f;
            public float Rozamiento { get => rozamiento; set => rozamiento = value; }
            private float grados;
            public float Grados { get => grados; set => grados = value; }
            private float velocidad = 0;
            public float Velocidad
            {
                get => FastMath.Min(FastMath.Max((velocidad + (aceleracion * tiempoBotonApretado) - (rozamiento * velocidad)), velocidadMinima), velocidadMaxima);
                set => velocidad = value;
            }
            public TGCVector3 versorDirector()
            {
                return new TGCVector3(FastMath.Cos(4.71238898f + grados), 0, FastMath.Sin(4.71238898f + grados));
            }

            public float giroTotal()
            {
                return gradosGiro * (velocidad / 10);
            }

            //MOVIMIENTO
            public void acelera()
            {
                aceleracion += 0.02f;
                direccion = 1;
            }
            public void frena()
            {
                rozamiento += 0.010f;
                if (velocidad < 0.05f)
                {
                    velocidad = 0;
                }
            }
            public void marchaAtras()
            {
                aceleracion -= 0.02f;
                direccion = -1;
            }
            public void giraDerecha()
            {
                grados -= this.giroTotal();
                this.RotateY(+giroTotal());
            }
            public void giraIzquierda()
            {
                grados += this.giroTotal();
                this.RotateY(-giroTotal());
            }
            public void parado()
            {
                this.RotateY(0);
                aceleracion = 0;
                rozamiento = 0.005f;
            }
            public void moverse()
            {
                this.Move(this.versorDirector() * velocidad);
            }

            internal TgcMesh Mesh;
        }


        //Camaras
        private TgcCamera camaraAerea;
        private CamaraAtrasRotadora camaraAtras;
        private TgcCamera camaraAereaFija;

        public override void Init()
        {
            //Device de DirectX para crear primitivas.
            var d3dDevice = D3DDevice.Instance.Device;

            //Objetos
            // Piso = new TgcSceneLoader().loadSceneFromFile(MediaDir + "Piso-TgcScene.xml").Meshes[0];
            // Pared = new TgcSceneLoader().loadSceneFromFile(MediaDir + "Pared-TgcScene.xml").Meshes[0];
            // Tribuna = new TgcSceneLoader().loadSceneFromFile(MediaDir + "Tribuna-TgcScene.xml").Meshes[0];  Resta definir si Estadio o Ciudad
            // Automotor = new TgcSceneLoader().loadSceneFromFile(MediaDir + "Auto-TgcScene.xml").Meshes[0];
            Ciudad = new TgcSceneLoader().loadSceneFromFile(MediaDir + "escena tp-TgcScene.xml");
            //TgcMesh conversion = Automotor2;
            //conversion= new TgcSceneLoader().loadSceneFromFile(MediaDir + "Auto-TgcScene.xml").Meshes[0];
            Automotor2.Mesh = new TgcSceneLoader().loadSceneFromFile(MediaDir + "Auto-TgcScene.xml").Meshes[0];

        }


        public override void Update()
        {
            PreUpdate();
            //Obtenemos acceso al objeto que maneja input de mouse y teclado del framework
            var input = Input;     

            //Cosas de Cámaras.
            var posicionCamaraArea = new TGCVector3(50, 2900, 0);
            var objetivoCamaraAerea = TGCVector3.Empty;
            camaraAerea = new TgcCamera();
            camaraAerea.SetCamera(posicionCamaraArea, objetivoCamaraAerea);

            camaraAereaFija = new TgcCamera();
            camaraAereaFija.SetCamera(posicionCamaraArea, Automotor2.Position);

            camaraAtras = new CamaraAtrasRotadora(Automotor2);
            camaraAtras.SetCamera(camaraAtras.PosicionCamaraAtras, Automotor2.Position);
            
            

            //Selección de Cámaras. (FALTA TERMINAR).
            if (input.keyDown(Key.D1))
            {
                Camara = camaraAtras;
            }
            else if (input.keyDown(Key.D2))
            {
                Camara = camaraAerea;
            }
            else if (input.keyDown(Key.D3))
            {
                Camara = camaraAereaFija;
            }
            else
            {
                Camara = camaraAtras;
            }

            //Movimiento del Automotor.
            if (input.keyDown(Key.Left) || input.keyDown(Key.A))
            {
                Automotor2.giraIzquierda();
            }
            else if (input.keyDown(Key.Right) || input.keyDown(Key.D))
            {
                Automotor2.giraDerecha();
            }
            if (input.keyDown(Key.Up) || input.keyDown(Key.W))
            {
                Automotor2.acelera();
                Automotor2.TiempoBotonApretado = ElapsedTime;

            }
            else if ((input.keyDown(Key.Down) || input.keyDown(Key.S)) && Automotor2.Velocidad <= 0)
            {
                Automotor2.marchaAtras();
                Automotor2.TiempoBotonApretado = ElapsedTime;
            }
            else
            {
                Automotor2.parado();
            }

            if (input.keyDown(Key.RightControl))

            {
                Automotor2.TiempoBotonApretado = ElapsedTime;
                Automotor2.frena();
            }

            PostUpdate();
        }

        public override void Render()
        {
            //Inicio el render de la escena, para ejemplos simples. Cuando tenemos postprocesado o shaders es mejor realizar las operaciones según nuestra conveniencia.
            PreRender();

            //Textos en pantalla.
            DrawText.drawText("Dirección en X :" + Automotor2.versorDirector().X, 0, 20, Color.OrangeRed);
            DrawText.drawText("Dirección en Z :" + Automotor2.versorDirector().Z, 0, 30, Color.OrangeRed);
            DrawText.drawText("Posición en X :" + Automotor2.Position.X, 0, 50, Color.Green);
            DrawText.drawText("Posición en Z :" + Automotor2.Position.Z, 0, 60, Color.Green);
            DrawText.drawText("Velocidad en X :" + Automotor2.Velocidad * 15 + "Km/h", 0, 80, Color.Yellow);
            DrawText.drawText("Mantega el botón 2 para ver cámara aérea.", 0, 100, Color.White);
            DrawText.drawText("Mantega el botón 3 para ver cámara aérea fija.", 0, 115, Color.White);
            DrawText.drawText("ACELERA :                     FLECHA ARRIBA", 1500, 10, Color.Black);
            DrawText.drawText("DOBLA DERECHA :           FLECHA DERECHA", 1500, 25, Color.Black);
            DrawText.drawText("DOBLA IZQUIERDA :         FLECHA IZQUIERDA", 1500, 40, Color.Black);
            DrawText.drawText("MARCHA ATRÁS :            FLECHA ABAJO", 1500, 60, Color.Black);
            DrawText.drawText("FRENO :                        CONTROL DERECHO", 1500, 80, Color.Black);

            //Render Objetos.

            //Pared.Render();
            //Tribuna.Render();
            //Piso.Render();
            //Box.Render();

            Automotor2.Render();
            Ciudad.RenderAll();

            //Finaliza el render y presenta en pantalla, al igual que el preRender se debe para casos puntuales es mejor utilizar a mano las operaciones de EndScene y PresentScene
            PostRender();
        }


        public override void Dispose()
        {
            //Box.Dispose();
            //Piso.Dispose();
            //Pared.Render();
            //Tribuna.Render();

            Automotor2.Dispose();
            Ciudad.DisposeAll();
        }
    }
}