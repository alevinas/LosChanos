using System;
using Microsoft.DirectX.DirectInput;
using BulletSharp;
using System.Collections.Generic;
using TGC.Core.BulletPhysics;
using TGC.Core.Direct3D;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;
using TGC.Core.Geometry;


namespace TGC.Group.Model
{
    public class FisicaMundo
    {
        private DiscreteDynamicsWorld dynamicsWorld;
        private CollisionDispatcher dispatcher;
        private DefaultCollisionConfiguration collisionConfiguration;
        private SequentialImpulseConstraintSolver constraintSolver;
        private BroadphaseInterface overlappingPairCache;

        private List<TgcMesh> Edificios = new List<TgcMesh>();
        private RigidBody piso;
        private TgcMesh auto { get; set; }
        private RigidBody cuerpoAuto;
        private TGCVector3 adelante;
        private TGCVector3 izquierda_derecha;

        public void cargarEdificios(List<TgcMesh> meshes)
        {
            this.Edificios = meshes;
        }

        public TgcMesh devolverAuto()
        {
            return auto;
        }

        public virtual void Init(string MediaDir)
        {
            collisionConfiguration = new DefaultCollisionConfiguration();
            dispatcher = new CollisionDispatcher(collisionConfiguration);
            GImpactCollisionAlgorithm.RegisterAlgorithm(dispatcher);
            constraintSolver = new SequentialImpulseConstraintSolver();
            overlappingPairCache = new DbvtBroadphase(); //AxisSweep3(new BsVector3(-5000f, -5000f, -5000f), new BsVector3(5000f, 5000f, 5000f), 8192);
            dynamicsWorld = new DiscreteDynamicsWorld(dispatcher, overlappingPairCache, constraintSolver, collisionConfiguration);
            dynamicsWorld.Gravity = new TGCVector3(0, -100f, 0).ToBulletVector3();

            foreach (var mesh in Edificios)
            {
                var objetos = BulletRigidBodyFactory.Instance.CreateRigidBodyFromTgcMesh(mesh);
                dynamicsWorld.AddRigidBody(objetos);
            }

            // Estructura del piso
            var cuerpoPiso = new StaticPlaneShape(TGCVector3.Up.ToBulletVector3(), 10);
            cuerpoPiso.LocalScaling = new TGCVector3().ToBulletVector3();
            var movimientoPiso = new DefaultMotionState();
            var pisoConstruccion = new RigidBodyConstructionInfo(0, movimientoPiso, cuerpoPiso);
            piso = new RigidBody(pisoConstruccion);
            piso.Friction = 0.00001f;
            piso.RollingFriction = 1;
            piso.Restitution = 1f;
            piso.UserObject = "floorBody";
            dynamicsWorld.AddRigidBody(piso);

            //Estructura del auto (Hacemos como una caja con textura)
            var loader = new TgcSceneLoader();

            TgcTexture texture = TgcTexture.createTexture(D3DDevice.Instance.Device, MediaDir + @"Textures\box4.jpg");
            TGCBox boxMesh1 = TGCBox.fromSize(new TGCVector3(20, 20, 20), texture);
            boxMesh1.Position = new TGCVector3(0, 10, 0);
            auto = boxMesh1.ToMesh("box");
            boxMesh1.Dispose();

            var tamañoAuto = new TGCVector3(55, 20, 80);
            cuerpoAuto = BulletRigidBodyFactory.Instance.CreateBox(tamañoAuto, 10, auto.Position, 0, 0, 0, 0, true);
            cuerpoAuto.Restitution = 0;
            cuerpoAuto.Gravity = new TGCVector3(0, -100f, 0).ToBulletVector3();
            dynamicsWorld.AddRigidBody(cuerpoAuto);

            auto = loader.loadSceneFromFile(MediaDir + "Auto-TgcScene.xml").Meshes[0];
        }

        public void Update(TgcD3dInput input)
        {
            var fuerza = 30.30f;
            dynamicsWorld.StepSimulation(1 / 60f, 100);
        
            if (input.keyDown(Key.UpArrow))
            {
                //Activa el comportamiento de la simulacion fisica para la capsula
                cuerpoAuto.ActivationState = ActivationState.ActiveTag;
                cuerpoAuto.AngularVelocity = TGCVector3.Empty.ToBulletVector3();
                cuerpoAuto.ApplyCentralImpulse(-fuerza * adelante.ToBulletVector3());
            }
        }

        public void Render(float tiempo)
        {
            //Hacemos render de la escena.
            foreach (var mesh in Edificios) mesh.Render();

            //Se hace el transform a la posicion que devuelve el el Rigid Body del Hummer
            auto.Position = new TGCVector3(cuerpoAuto.CenterOfMassPosition.X, cuerpoAuto.CenterOfMassPosition.Y + 0, cuerpoAuto.CenterOfMassPosition.Z);
            auto.Transform = TGCMatrix.Translation(cuerpoAuto.CenterOfMassPosition.X, cuerpoAuto.CenterOfMassPosition.Y, cuerpoAuto.CenterOfMassPosition.Z);
            auto.Render();
        }

        public void Dispose()
        {
            //Se hace dispose del modelo fisico.
            dynamicsWorld.Dispose();
            dispatcher.Dispose();
            collisionConfiguration.Dispose();
            constraintSolver.Dispose();
            overlappingPairCache.Dispose();
            piso.Dispose();

            //Dispose de Meshes
            foreach (TgcMesh mesh in Edificios) mesh.Dispose();

        }
    }
}
