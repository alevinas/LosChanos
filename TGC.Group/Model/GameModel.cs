using Microsoft.DirectX.DirectInput;
using System.Collections.Generic;
using System.Drawing;
using TGC.Core.BulletPhysics;
using BulletSharp.Math;
using BulletSharp.SoftBody;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Sound;
using TGC.Core.Terrain;
using TGC.Core.Textures;
using TGC.Core.Geometry;
using Microsoft.DirectX.Direct3D;
using TGC.Core.Collision;
using TGC.Core.Shaders;
using TGC.Core.BoundingVolumes;
using Shader = Microsoft.DirectX.Direct3D.Effect;

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

        //Declaro Cosas del Escenario
        private TgcScene Plaza { get; set; }
        private List<TgcMesh> MayasAutoFisico1 { get; set; }
        private List<TgcMesh> MayasAutoFisico2 { get; set; }
        private AutoManejable AutoFisico1 { get; set; }
        private List<TgcMesh> MayasIA { get; set; }
        private AutoManejable AutoFisico2 { get; set; }
        public PoliciasIA GrupoPolicias { get; set; }

        // Fisica del Mundo 
        private FisicaMundo Fisica;
        private TgcSkyBox Cielo;

        //Camaras
        private AutoManejable JugadorActivo { get; set; }
        private CamaraAtrasAF Camara01 { get; set; }
        private CamaraAtrasAF Camara02 { get; set; }
        private CamaraEspectador Camara03 { get; set; }

        // Declaro Emisor de particulas
        public string PathHumo { get; set; }

        //SONIDO ///////////
        //Ambiente
        private TgcStaticSound Musica;
        private TgcStaticSound Tribuna;

        // Colisiones
        private bool Choque { get; set; }
        private bool inGame { get; set; }

        ////////////////////////////////////////////

        int SwitchMusica { get; set; }
        int SwitchFX { get; set; }
        int SwitchInicio { get; set; }
        int SwitchCamara { get; set; }
        int SwitchInvisibilidadJ1 { get; set; }
        int SwitchInvisibilidadJ2 { get; set; }

        public AutoManejable[] Jugadores { get; set; }
        private List<AutoManejable> Players { get; set; }
        private List<AutoIA> Policias { get; set; }

        public Shader Invisibilidad { get; set; }
        public Shader EnvMap { get; set; }
        public float Tiempo { get; set; }
        private Surface g_pDepthStencil;
        private Texture g_pRenderTarget;
        private VertexBuffer g_pVBV3D;

        private TGCVector3 posicionLuz;

        public bool juegoDoble = false;
        public bool pantallaDoble = false;

        public Texture RenderTarget;
        public Hud Hud;

        public override void Init()
        {
            Tiempo = 0;
            var d3dDevice = D3DDevice.Instance.Device;

            Plaza = new TgcSceneLoader().loadSceneFromFile(MediaDir + "Plaza-TgcScene.xml");
            MayasIA= new TgcSceneLoader().loadSceneFromFile(MediaDir + "AutoPolicia-TgcScene.xml").Meshes;
            MayasAutoFisico1 = new TgcSceneLoader().loadSceneFromFile(MediaDir + "AutoAmarillo-TgcScene.xml").Meshes;
            MayasAutoFisico2 = new TgcSceneLoader().loadSceneFromFile(MediaDir + "AutoNaranja-TgcScene.xml").Meshes;
            PathHumo = MediaDir + "Textures\\TexturaHumo.png";

            //Shader Invisibilidad
            Invisibilidad = TGCShaders.Instance.LoadEffect(ShadersDir + "\\Invisibilidad.fx");
            Invisibilidad.Technique = "DefaultTechnique";

            g_pDepthStencil = d3dDevice.CreateDepthStencilSurface(d3dDevice.PresentationParameters.BackBufferWidth,
               d3dDevice.PresentationParameters.BackBufferHeight,
               DepthFormat.D24S8, MultiSampleType.None, 0, true);

            g_pRenderTarget = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget, Format.X8R8G8B8,
                Pool.Default);

            Invisibilidad.SetValue("g_RenderTarget", g_pRenderTarget);

            // Resolucion de pantalla
            Invisibilidad.SetValue("screen_dx", d3dDevice.PresentationParameters.BackBufferWidth);
            Invisibilidad.SetValue("screen_dy", d3dDevice.PresentationParameters.BackBufferHeight);

            CustomVertex.PositionTextured[] vertices =
            {
                new CustomVertex.PositionTextured(-1, 1, 1, 0, 0),
                new CustomVertex.PositionTextured(1, 1, 1, 1, 0),
                new CustomVertex.PositionTextured(-1, -1, 1, 0, 1),
                new CustomVertex.PositionTextured(1, -1, 1, 1, 1)
            };
            //Vertex buffer de los triangulos
            g_pVBV3D = new VertexBuffer(typeof(CustomVertex.PositionTextured),
                4, d3dDevice, Usage.Dynamic | Usage.WriteOnly,
                CustomVertex.PositionTextured.Format, Pool.Default);
            g_pVBV3D.SetData(vertices, 0, LockFlags.None);

            // --------------------------------------------------------------------
            //Shader EnvMap
            EnvMap = TGCShaders.Instance.LoadEffect(ShadersDir + "\\EnvMap.fx");
            EnvMap.Technique = "RenderScene";
            posicionLuz = new TGCVector3(0, 600, 0);

            // --------------------------------------------------------------------

            //Cielo
            Cielo = new TgcSkyBox
            {
                Center = TGCVector3.Empty,
                Size = new TGCVector3(10000, 10000, 10000)
            };
            var cieloPath = MediaDir + "Cielo\\";

            Cielo.setFaceTexture(TgcSkyBox.SkyFaces.Up, cieloPath + "cloudtop_up.jpg");
            Cielo.setFaceTexture(TgcSkyBox.SkyFaces.Down, cieloPath + "cloudtop_down.jpg");
            Cielo.setFaceTexture(TgcSkyBox.SkyFaces.Left, cieloPath + "cloudtop_left.jpg");
            Cielo.setFaceTexture(TgcSkyBox.SkyFaces.Right, cieloPath + "cloudtop_right.jpg");
            Cielo.setFaceTexture(TgcSkyBox.SkyFaces.Front, cieloPath + "cloudtop_front.jpg");
            Cielo.setFaceTexture(TgcSkyBox.SkyFaces.Back, cieloPath + "cloudtop_back.jpg");

            Cielo.SkyEpsilon = 11f;
            Cielo.Init();


            // Implemento la fisica 
            Fisica = new FisicaMundo();
            for (int i = 30; i<238; i++)
            {
                var objetos = BulletRigidBodyFactory.Instance.CreateRigidBodyFromTgcMesh(Plaza.Meshes[i]);
                Fisica.dynamicsWorld.AddRigidBody(objetos);
            }


            // Inicializo los coches
            AutoFisico1 = new AutoManejable(MayasAutoFisico1, new TGCVector3(-1000, 0, 3500),270,Fisica,PathHumo,MediaDir, DirectSound.DsDevice);
            AutoFisico2 = new AutoManejable(MayasAutoFisico2, new TGCVector3(4000, 0, 3500), 270, Fisica, PathHumo,MediaDir, DirectSound.DsDevice);
            AutoFisico2.ConfigurarTeclas(Key.W, Key.S, Key.D, Key.A, Key.LeftControl, Key.Tab);
            AutoFisico1.ConfigurarTeclas(Key.UpArrow, Key.DownArrow, Key.RightArrow, Key.LeftArrow, Key.RightControl, Key.Space);
            AutoFisico1.Vida = 1000;
            AutoFisico2.Vida = 1000;
            Jugadores = new[] { AutoFisico1, AutoFisico2 };
            GrupoPolicias = new PoliciasIA(MayasIA, Fisica, PathHumo, Jugadores, MediaDir, DirectSound.DsDevice);
            Players = new List<AutoManejable> { AutoFisico1, AutoFisico2 }; // Para el sonido y las colisiones

            // Sonidos
            int volumen1 = -1800;  // RANGO DEL 0 AL -10000 (Silenciado al -10000)
            var pathMusica = MediaDir + "Musica\\Running90s.wav";
            Musica = new TgcStaticSound();
            Musica.loadSound(pathMusica, volumen1, DirectSound.DsDevice);

            int volumen2 = -400;
            var pathTribuna = MediaDir + "Musica\\Tribuna.wav";
            Tribuna = new TgcStaticSound();
            Tribuna.loadSound(pathTribuna, volumen2, DirectSound.DsDevice);

            // Jugadores
            foreach (var auto in Players)
            {
                
                auto.sonidoAceleracion = new TgcStaticSound();
                auto.sonidoDesaceleracion = new TgcStaticSound();
                auto.frenada = new TgcStaticSound();
                auto.choque = new TgcStaticSound();

                auto.sonidoDesaceleracion.loadSound(MediaDir + "Musica\\Desacelerando.wav", -2000, DirectSound.DsDevice);
                auto.sonidoAceleracion.loadSound(MediaDir + "Musica\\Motor1.wav", -2000, DirectSound.DsDevice);
                auto.frenada.loadSound(MediaDir + "Musica\\Frenada.wav", -2000, DirectSound.DsDevice);
                auto.choque.loadSound(MediaDir + "Musica\\Choque1.wav", -2000, DirectSound.DsDevice);

            }

            SwitchInicio = 1;
            SwitchCamara = 1;
            Hud = new Hud(MediaDir, Jugadores);
        }


        public override void Update()
        {
            PreUpdate();

            var input = Input;

            //Camaras
            Camara01 = new CamaraAtrasAF(AutoFisico1);
            Camara02 = new CamaraAtrasAF(AutoFisico2);
            Camara03 = new CamaraEspectador();

            GrupoPolicias.Update();
            AutoFisico1.Update(input);
            AutoFisico2.Update(input);

            //Colisiones entre los autos y los policias
            foreach (var Policia in GrupoPolicias.Todos)
            {
                if(TgcCollisionUtils.testAABBAABB(AutoFisico1.BBFinal,Policia.BBFinal) && inGame)
                {
                    AutoFisico1.choque.play(false);
                    AutoFisico1.Vida -= 5;
                }
                if(TgcCollisionUtils.testAABBAABB(AutoFisico2.BBFinal, Policia.BBFinal) && inGame)
                {
                    AutoFisico2.choque.play(false);
                    AutoFisico2.Vida -= 5;
                }
            }
            //Colisiones entre los autos y el escenario
            foreach (var mesh in Plaza.Meshes)
            {
                if(TgcCollisionUtils.testAABBAABB(AutoFisico1.BBFinal, mesh.BoundingBox) && inGame)
                {
                    AutoFisico1.choque.play(false);
                    AutoFisico1.Vida -= 5;
                }
                if(TgcCollisionUtils.testAABBAABB(AutoFisico2.BBFinal, mesh.BoundingBox) && inGame)
                {
                    AutoFisico2.choque.play(false);
                    AutoFisico2.Vida -= 5;
                }
            }

            switch (SwitchCamara)
            {
                case 1:
                    {
                        Camara = Camara01;
                        JugadorActivo = AutoFisico1;
                        pantallaDoble = false;
                        if (input.keyPressed(Key.F6) && juegoDoble)
                        {
                            SwitchCamara = 2;
                        }
                        else if (input.keyPressed(Key.F7))
                        {
                            SwitchCamara = 3;      
                        }
                        break;
                    }
                case 2:
                    {
                        Camara = Camara02;
                        JugadorActivo = AutoFisico2;
                        pantallaDoble = false;
                        if (input.keyPressed(Key.F5))
                        {
                            SwitchCamara = 1;
                        }
                        else if (input.keyPressed(Key.F7))
                        {
                            SwitchCamara = 3;
                        }
                        break;
                    }
                case 3:
                    {
                        Camara = Camara03;
                        pantallaDoble = true;
                        if (input.keyPressed(Key.F5))
                        {
                            SwitchCamara = 1;
                        }
                        else if (input.keyPressed(Key.F6) && juegoDoble)
                        {
                            SwitchCamara = 2;
                        }
                        break;
                    }
            }
            
            switch (SwitchMusica)
            {
                case 1:
                    {   
                        Musica.play(true);
                        if (Input.keyPressed(Key.F8))
                        {
                            SwitchMusica = 2;
                        }
                        break;
                    }
                case 2:
                    {
                        Musica.stop();
                        if (Input.keyPressed(Key.F8))
                        {
                            SwitchMusica = 1;
                        }
                            break;
                    }
            }
            switch (SwitchFX)
            {
                case 1:
                    {
                        Tribuna.play(true);
                        if (Input.keyPressed(Key.F9))
                        {
                            SwitchFX = 2;
                        }
                        break;
                    }
                case 2:
                    {
                        Tribuna.stop();
                        if (Input.keyPressed(Key.F9))
                        {
                            SwitchFX = 1;
                        }
                        break;
                    }
            }

            switch (SwitchInvisibilidadJ1)
            {
                case 1:
                    {
                        Jugadores[0] = AutoFisico1;
                        if (Input.keyPressed(Key.F3))
                        {
                            Jugadores[0].Invisible = true;
                            SwitchInvisibilidadJ1 = 2;
                        }
                        break;
                    }
                case 2:
                    {
                        Jugadores[0] = null;
                        if (Input.keyPressed(Key.F3))
                        {
                            AutoFisico1.Invisible = false;
                            SwitchInvisibilidadJ1 = 1;
                        }
                        break;
                    }
                default:
                    {
                        if (Input.keyPressed(Key.F3))
                        {
                            Jugadores[0].Invisible = true;
                            SwitchInvisibilidadJ1 = 2;
                        }
                        break;
                    }
            }
            if (juegoDoble)
            {
                switch (SwitchInvisibilidadJ2)
                {
                    case 1:
                        {
                            Jugadores[1] = AutoFisico2;
                            if (Input.keyPressed(Key.F4))
                            {
                                Jugadores[1].Invisible = true;
                                SwitchInvisibilidadJ2 = 2;
                            }
                            break;
                        }
                    case 2:
                        {
                            Jugadores[1] = null;
                            if (Input.keyPressed(Key.F4))
                            {
                                AutoFisico2.Invisible = false;
                                SwitchInvisibilidadJ2 = 1;
                            }
                            break;
                        }
                    default:
                        {
                            if (Input.keyPressed(Key.F4))
                            {
                                Jugadores[1].Invisible = true;
                                SwitchInvisibilidadJ2 = 2;
                            }
                            break;
                        }
                }
            }
            PostUpdate();
        }

        public override void Render()
        {

            PreRender();
            ClearTextures();

            bool invisibilidadActivada = (SwitchInvisibilidadJ1 - 1 == SwitchCamara) || (SwitchInvisibilidadJ2 == SwitchCamara) ;

            //Permito las particulas
            D3DDevice.Instance.ParticlesEnabled = true;
            D3DDevice.Instance.EnableParticles();


            switch (SwitchInicio)
            {
                case 1:
                    {
                        Hud.PantallaInicio();
                        if (Input.keyPressed(Key.C))
                        {
                            SwitchInicio = 2;
                            
                        }
                        if (Input.keyPressed(Key.D1))
                        {
                            Jugadores[1] = null;
                            SwitchInicio = 3;
                            SwitchMusica = 1;
                            SwitchFX = 1;
                            AutoFisico1.Encendido();
                            inGame = true;
                        }
                        if (Input.keyPressed(Key.D2))
                        {
                            juegoDoble = true;  
                            SwitchInicio = 3;
                            SwitchMusica = 1;
                            SwitchFX = 1;
                            AutoFisico1.Encendido();
                            AutoFisico2.Encendido();
                            inGame = true;

                        }
                        break;
                    }
                case 2:
                    {
                        Hud.PantallaControles();
                        if (Input.keyPressed(Key.V))
                        {
                            SwitchInicio = 1;
                        }
                        break;
                    }
                case 3:
                    {
                        var device = D3DDevice.Instance.Device;


                        Tiempo += ElapsedTime;
                        AutoFisico1.ElapsedTime = ElapsedTime;                      
                        //Cargar variables de shader

                        // dibujo la escena una textura
                        Invisibilidad.Technique = "DefaultTechnique";
                        // guardo el Render target anterior y seteo la textura como render target
                        var pOldRT = device.GetRenderTarget(0);
                        var pSurf = g_pRenderTarget.GetSurfaceLevel(0);
                        if (invisibilidadActivada)
                            device.SetRenderTarget(0, pSurf);
                        var pOldDS = device.DepthStencilSurface;

                        if (invisibilidadActivada)
                            device.DepthStencilSurface = g_pDepthStencil;

                        device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);

                        DrawText.drawText("Velocidad P1:" + AutoFisico1.Velocidad, 0, 90, Color.Green);
                        DrawText.drawText("Vida J1:" + AutoFisico1.Vida, 0, 110, Color.Green);
                        DrawText.drawText("Vida J2:" + AutoFisico2.Vida, 0, 120, Color.Green);
                        DrawText.drawText("Auto1: " + AutoFisico1.listBB.Count, 0, 150, Color.Black);

                        // MUESTRO LOS BB DE LA PLAZA
                        /*
                        foreach (var mesh in Plaza.Meshes)
                        {
                            mesh.BoundingBox.Render();
                        }
                        */
                        
                        // MUESTRO LOS BB DE LOS AUTOS
                        /*
                        foreach (var auto in Policias)
                        {
                            foreach(var mesh in auto.Mayas)
                            {
                                mesh.BoundingBox.Render();
                                mesh.BoundingBox.transform(auto.Movimiento); 
                            }
                        }
                        */
                        
                        if (juegoDoble)
                        {
                            AutoFisico2.ElapsedTime = ElapsedTime;
                            AutoFisico2.Render(ElapsedTime);
                        }

                        Plaza.RenderAll();
                        AutoFisico1.Render(ElapsedTime);
                        GrupoPolicias.Render(ElapsedTime);
                        Cielo.Render();

                        Hud.Juego(invisibilidadActivada,JugadorActivo,juegoDoble,pantallaDoble, AutoFisico1,AutoFisico2);
                        if (Input.keyDown(Key.F10))
                        {
                            Hud.Pausar();
                        }

                        pSurf.Dispose();

                        if (invisibilidadActivada)
                        {
                            device.DepthStencilSurface = pOldDS;
                            device.SetRenderTarget(0, pOldRT);
                            Invisibilidad.Technique = "PostProcess";
                            Invisibilidad.SetValue("time", Tiempo);
                            device.VertexFormat = CustomVertex.PositionTextured.Format;
                            device.SetStreamSource(0, g_pVBV3D, 0);
                            Invisibilidad.SetValue("g_RenderTarget", g_pRenderTarget);

                            RenderTarget = g_pRenderTarget;

                            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
                            Invisibilidad.Begin(FX.None);
                            Invisibilidad.BeginPass(0);
                            device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                            Invisibilidad.EndPass();
                            Invisibilidad.End();
                            RenderTarget = g_pRenderTarget;

                        }
                        RenderAxis();
                        RenderFPS();

                        if(AutoFisico1.Vida < 0)
                        {
                            SwitchInicio = 4;
                        }

                        // EnvMap
                        //D3DDevice.Instance.Device.EndScene();
                        var g_pCubeMap = new CubeTexture(D3DDevice.Instance.Device, 256, 1, Usage.RenderTarget, Format.A16B16G16R16F, Pool.Default);
                        var pOldRT2 = D3DDevice.Instance.Device.GetRenderTarget(0);
                        // ojo: es fundamental que el fov sea de 90 grados.
                        // asi que re-genero la matriz de proyeccion
                        D3DDevice.Instance.Device.Transform.Projection = TGCMatrix.PerspectiveFovLH(Geometry.DegreeToRadian(90.0f), 1f, 1f, 10000f).ToMatrix();

                        // Genero las caras del enviroment map
                        for (var nFace = CubeMapFace.PositiveX; nFace <= CubeMapFace.NegativeZ; ++nFace)
                        {
                            var pFace = g_pCubeMap.GetCubeMapSurface(nFace, 0);
                            D3DDevice.Instance.Device.SetRenderTarget(0, pFace);
                            TGCVector3 Dir, VUP;
                            Color color;
                            switch (nFace)
                            {
                                default:
                                case CubeMapFace.PositiveX:
                                    // Left
                                    Dir = new TGCVector3(1, 0, 0);
                                    VUP = TGCVector3.Up;
                                    color = Color.Black;
                                    break;

                                case CubeMapFace.NegativeX:
                                    // Right
                                    Dir = new TGCVector3(-1, 0, 0);
                                    VUP = TGCVector3.Up;
                                    color = Color.Red;
                                    break;

                                case CubeMapFace.PositiveY:
                                    // Up
                                    Dir = TGCVector3.Up;
                                    VUP = new TGCVector3(0, 0, -1);
                                    color = Color.Gray;
                                    break;

                                case CubeMapFace.NegativeY:
                                    // Down
                                    Dir = TGCVector3.Down;
                                    VUP = new TGCVector3(0, 0, 1);
                                    color = Color.Yellow;
                                    break;

                                case CubeMapFace.PositiveZ:
                                    // Front
                                    Dir = new TGCVector3(0, 0, 1);
                                    VUP = TGCVector3.Up;
                                    color = Color.Green;
                                    break;

                                case CubeMapFace.NegativeZ:
                                    // Back
                                    Dir = new TGCVector3(0, 0, -1);
                                    VUP = TGCVector3.Up;
                                    color = Color.Blue;
                                    break;
                            }

                            D3DDevice.Instance.Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, color, 1.0f, 0);
                            //D3DDevice.Instance.Device.BeginScene();

                            //Renderizar
                            
                            foreach(var mesh in AutoFisico1.Mayas)
                            {
                                mesh.Effect = EnvMap;
                                mesh.Technique = "RenderScene";
                                mesh.Render();
                            }

                            //D3DDevice.Instance.Device.EndScene();
                        }
                        // restuaro el render target
                        D3DDevice.Instance.Device.SetRenderTarget(0, pOldRT);
                        //TextureLoader.Save("test.bmp", ImageFileFormat.Bmp, g_pCubeMap);

                        // Restauro el estado de las transformaciones
                       
                        D3DDevice.Instance.Device.Transform.View = Camara.GetViewMatrix().ToMatrix();
                        D3DDevice.Instance.Device.Transform.Projection = TGCMatrix.PerspectiveFovLH(Geometry.DegreeToRadian(45.0f), D3DDevice.Instance.AspectRatio, 1f, 10000f).ToMatrix();

                        // dibujo pp dicho
                        //D3DDevice.Instance.Device.BeginScene();
                        D3DDevice.Instance.Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
                        EnvMap.SetValue("g_txCubeMap", g_pCubeMap);

                        //renderScene(ElapsedTime, false);
                        foreach (var mesh in AutoFisico1.Mayas)
                        {
                            mesh.Effect = EnvMap;
                            mesh.Technique = "RenderScene";
                            mesh.Render();
                        }

                        g_pCubeMap.Dispose();

                        //D3DDevice.Instance.Device.EndScene();
                        //D3DDevice.Instance.Device.Present();


                        break;
                    }
                case 4:
                    {
                        Hud.JuegoTerminado();
                        if (Input.keyPressed(Key.M))
                        {
                            SwitchInicio = 1;
                        }
                        break;
                    }
            }

            PostRender();
        }

        public override void Dispose()
        {
            Plaza.DisposeAll();
            AutoFisico1.Dispose();
            GrupoPolicias.Dispose();
            Cielo.Dispose();
            Musica.dispose();
            Tribuna.dispose();
            Hud.Dispose();

            foreach (var auto in Players)
            {
                auto.sonidoAceleracion.dispose();
                auto.sonidoDesaceleracion.dispose();
                auto.frenada.dispose();

            }

            Invisibilidad.Dispose();
            g_pRenderTarget.Dispose();
            g_pVBV3D.Dispose();
            g_pDepthStencil.Dispose();

        }

    }

    }
    

