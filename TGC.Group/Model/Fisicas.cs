﻿using System;
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
            piso.Friction = 0.8f;
            piso.RollingFriction = 1;
            piso.Restitution = 0.8f;
            piso.UserObject = "floorBody";
            dynamicsWorld.AddRigidBody(piso);

            //Estructura del auto (Hacemos como una caja con textura)
            var loader = new TgcSceneLoader();

            TgcTexture texture = TgcTexture.createTexture(D3DDevice.Instance.Device, MediaDir + @"Textures\box4.jpg");
            TGCBox boxMesh1 = TGCBox.fromSize(new TGCVector3(20, 20, 20), texture);
            boxMesh1.Position = new TGCVector3(0, 10, 0);
            auto = boxMesh1.ToMesh("box");
            boxMesh1.Dispose();

            var tamañoAuto = new TGCVector3(55, 80, 0);
            cuerpoAuto = BulletRigidBodyFactory.Instance.CreateBox(tamañoAuto, 10, auto.Position, 0, 0, 0, 0.55f, true);
            cuerpoAuto.Restitution = 0.5f;
            cuerpoAuto.Gravity = new TGCVector3(0, -100f, 0).ToBulletVector3();
            dynamicsWorld.AddRigidBody(cuerpoAuto);

            //auto = loader.loadSceneFromFile(MediaDir + "Auto-TgcScene.xml").Meshes[0];
            /*
                    Solo va a estar cargando una caja, no el auto todavia porque la interacción con el piso todavia 
                    es errática y hay que ajustar parámetros. Además, la clase AutoManejable recibe un TgcScene (el auto
                    por ser una scene que tenga una lista de meshs incluidas, que sean las ruedas) y Bullet solo se maneja 
                    con Mesh, y es todo un problema adaptar toda la clase. No lo hicimos por falta de tiempo. Por esta razon
                    los dos coches sheriff no tienen interacción (Bullet aplicado) con el entorno. Pendiente a adaptar.

                    A menos que haya una manera de que Bullet maneje una Scene completa, sin necesidad de hacer un foreach.

            
             */

            //Vectores de la direccion del auto post-choque
            adelante = new TGCVector3(0, 0, 1);
            izquierda_derecha = new TGCVector3(1, 0, 0);
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
            else if (input.keyDown(Key.LeftArrow))
            {
                cuerpoAuto.ActivationState = ActivationState.ActiveTag;
                cuerpoAuto.AngularVelocity = TGCVector3.Empty.ToBulletVector3();
                cuerpoAuto.ApplyCentralImpulse(fuerza * izquierda_derecha.ToBulletVector3());
            }
            else if (input.keyDown(Key.RightArrow))
            {
                cuerpoAuto.ActivationState = ActivationState.ActiveTag;
                cuerpoAuto.AngularVelocity = TGCVector3.Empty.ToBulletVector3();
                cuerpoAuto.ApplyCentralImpulse(-fuerza * izquierda_derecha.ToBulletVector3());
            }
            else if (input.keyDown(Key.DownArrow))
            {
                cuerpoAuto.ActivationState = ActivationState.ActiveTag;
                cuerpoAuto.AngularVelocity = TGCVector3.Empty.ToBulletVector3();
                cuerpoAuto.ApplyCentralImpulse(fuerza * adelante.ToBulletVector3());
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
