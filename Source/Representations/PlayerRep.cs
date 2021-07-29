﻿using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Facepunch.Steamworks;
using Facepunch.Steamworks.Data;
using MelonLoader;
using ModThatIsNotMod;
using MultiplayerMod.Boneworks;
using MultiplayerMod.Core;
using MultiplayerMod.Features;
using MultiplayerMod.Networking;
using MultiplayerMod.Structs;
using StressLevelZero.Combat;
using StressLevelZero.Data;
using StressLevelZero.Props.Weapons;
using StressLevelZero.UI.Radial;
using TMPro;
using UnityEngine;
using static UnityEngine.Object;

namespace MultiplayerMod.Representations
{
    public class PlayerRep
    {
        public GameObject ford;
        public GameObject head;
        public GameObject handL;
        public GameObject handR;
        public GameObject pelvis;
        public GameObject nametag;
        public GameObject footL;
        public GameObject footR;
        public GameObject namePlate;
        public BoneworksRigTransforms rigTransforms;
        public GameObject rightGun;
        public Gun rightGunScript;
        public BulletObject rightBulletObject;
        public GameObject gunRParent;

        public GameObject leftGun;
        public Gun leftGunScript;
        public BulletObject leftBulletObject;
        public GameObject gunLParent;
        public FaceAnimator faceAnimator;

        public MPPlayer player;

        public event Action<float, PlayerRep> OnDamage;

        public static AssetBundle fordBundle;

        public static void LoadFord()
        {
            fordBundle = AssetBundle.LoadFromFile("ford.ford");
            if (fordBundle == null)
                MelonLogger.Error("Failed to load Ford asset bundle");

            GameObject fordPrefab = fordBundle.LoadAsset("Assets/Ford.prefab").Cast<GameObject>();
            if (fordPrefab == null)
                MelonLogger.Error("Failed to load Ford from the asset bundle???");
        }

        // Constructor
        public PlayerRep(string name, MPPlayer player)
        {
            GameObject ford = Instantiate(fordBundle.LoadAsset("Assets/Ford.prefab").Cast<GameObject>());

            this.player = player;

            Rigidbody rb = ford.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            // Set up damage receiver
            GenericAttackReceiver attackReceiver = ford.AddComponent<GenericAttackReceiver>();
            attackReceiver.AttackEvent = new UnityEventFloat();
            Action<float> attackEvent = (float damage) => { OnDamage?.Invoke(damage, this); };
            attackReceiver.AttackEvent.AddListener(attackEvent);
            

            // Don't destroy on level load so we can be lazy and avoid recreating the player reps.
            DontDestroyOnLoad(ford);

            // Set up impact properties (so the rep can be stabbed and receive damage)
            ImpactPropertiesManager bloodManager = ford.AddComponent<ImpactPropertiesManager>();
            bloodManager.material = ImpactPropertiesVariables.Material.Blood;
            bloodManager.modelType = ImpactPropertiesVariables.ModelType.Skinned;
            bloodManager.MainColor = UnityEngine.Color.red;
            bloodManager.SecondaryColor = UnityEngine.Color.red;
            bloodManager.PenetrationResistance = 0.8f;
            bloodManager.megaPascalModifier = 1;
            bloodManager.FireResistance = 100;

            Collider[] colliders = ford.GetComponentsInChildren<Collider>();
            ImpactProperties blood = ford.AddComponent<ImpactProperties>();
            blood.material = ImpactPropertiesVariables.Material.Blood;
            blood.modelType = ImpactPropertiesVariables.ModelType.Skinned;
            blood.MainColor = UnityEngine.Color.red;
            blood.SecondaryColor = UnityEngine.Color.red;
            blood.PenetrationResistance = 0.8f;
            blood.megaPascalModifier = 1;
            blood.FireResistance = 100;
            blood.MyCollider = colliders[0];
            blood.hasManager = true;
            blood.Manager = bloodManager;

            GameObject root = ford.transform.Find("Ford/Brett@neutral").gameObject; // Get the root of the model

            faceAnimator = new FaceAnimator
            {
                animator = root.GetComponent<Animator>(),
                faceTime = 10
            };

            Transform realRoot = root.transform.Find("SHJntGrp/MAINSHJnt/ROOTSHJnt"); // Then get the root of the rig

            // Create an anchor object to hold the rep's gun
            gunRParent = new GameObject("gunRParent");
            gunRParent.transform.parent = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/r_Arm_ClavicleSHJnt/r_AC_AuxSHJnt/r_Arm_ShoulderSHJnt/r_Arm_Elbow_CurveSHJnt/r_WristSHJnt/r_Hand_1SHJnt");
            gunRParent.transform.localPosition = new Vector3(0.0758f, -0.0459f, -0.0837f);
            gunRParent.transform.localEulerAngles = new Vector3(2.545f, -251.689f, 149.121f);

            GameObject rig = Player.GetRigManager();
            PopUpMenuView menu = rig.GetComponentInChildren<PopUpMenuView>();
            GameObject spawnGun = menu.utilityGunSpawnable.prefab;
            SpawnableMasterListData masterList = spawnGun.GetComponent<SpawnGun>().masterList;
            rightGun = Instantiate(masterList.objects[BWUtil.gunOffset].prefab.transform.Find("Physics/Root/Gun").gameObject);
            rightGun.GetComponent<Rigidbody>().isKinematic = true;
            rightGun.transform.parent = gunRParent.transform;
            rightGun.transform.localPosition = Vector3.zero;
            rightGun.transform.localRotation = Quaternion.identity;
            rightGunScript = rightGun.GetComponent<Gun>();
            rightGunScript.proxyOverride = null;
            rightBulletObject = rightGunScript.overrideMagazine.AmmoSlots[0];
            rightGunScript.roundsPerMinute = 20000;
            rightGunScript.roundsPerSecond = 333;
            GameObject.Destroy(rightGun.GetComponent<ConfigurableJoint>());
            GameObject.Destroy(rightGun.GetComponent<ImpactProperties>());
            GameObject.Destroy(rightGun.transform.Find("attachment_Lazer_Omni").gameObject);

            // Create an anchor object to hold the rep's gun
            gunLParent = new GameObject("gunLParent");
            gunLParent.transform.parent = realRoot.Find("Spine_01SHJnt/Spine_02SHJnt/Spine_TopSHJnt/l_Arm_ClavicleSHJnt/l_AC_AuxSHJnt/l_Arm_ShoulderSHJnt/l_Arm_Elbow_CurveSHJnt/l_WristSHJnt/l_Hand_1SHJnt");
            gunLParent.transform.localPosition = new Vector3(-0.0941f, 0.0452f, 0.0945f);
            gunLParent.transform.localEulerAngles = new Vector3(3.711f, -81.86301f, -157.739f);

            leftGun = Instantiate(masterList.objects[BWUtil.gunOffset].prefab.transform.Find("Physics/Root/Gun").gameObject);
            leftGun.GetComponent<Rigidbody>().isKinematic = true;
            leftGun.transform.parent = gunLParent.transform;
            leftGun.transform.localPosition = Vector3.zero;
            leftGun.transform.localRotation = Quaternion.identity;
            leftGunScript = leftGun.GetComponent<Gun>();
            leftGunScript.proxyOverride = null;
            leftBulletObject = leftGunScript.overrideMagazine.AmmoSlots[0];
            GameObject.Destroy(leftGun.GetComponent<ConfigurableJoint>());
            GameObject.Destroy(leftGun.transform.Find("attachment_Lazer_Omni").gameObject);
            GameObject.Destroy(leftGun.GetComponent<ImpactProperties>());

            foreach (var renderer in gunLParent.GetComponentsInChildren<Renderer>())
                renderer.enabled = false;

            foreach (var renderer in gunRParent.GetComponentsInChildren<Renderer>())
                renderer.enabled = false;

            foreach (var col in gunLParent.GetComponentsInChildren<Collider>())
                Destroy(col);

            foreach (var col in gunRParent.GetComponentsInChildren<Collider>())
                Destroy(col);

            // Assign the transforms for the rep
            rigTransforms = BWUtil.GetHumanoidRigTransforms(root);

            // Grab these body parts from the rigTransforms
            head = rigTransforms.neck.gameObject;
            handL = rigTransforms.lWrist.gameObject;
            handR = rigTransforms.rWrist.gameObject;
            pelvis = rigTransforms.spine1.gameObject;

            // Create the nameplate and assign values to the TMP's vars
            namePlate = new GameObject("Nameplate");
            TextMeshPro tm = namePlate.AddComponent<TextMeshPro>();
            tm.text = name;
            tm.color = UnityEngine.Color.green;
            tm.alignment = TextAlignmentOptions.Center;
            tm.fontSize = 1.0f;

            // Prevents the nameplate from being destroyed during a level change
            DontDestroyOnLoad(namePlate);

            MelonCoroutines.Start(AsyncAvatarRoutine(player.FullID));

            // Gives certain users special appearances
            Extras.SpecialUsers.GiveUniqueAppearances(player.FullID, realRoot, tm);

            this.ford = ford;
        }

        private IEnumerator AsyncAvatarRoutine(SteamId id)
        {
            Task<Image?> imageTask = SteamFriends.GetLargeAvatarAsync(id);
            while (!imageTask.IsCompleted)
            {
                yield return null;
            }

            if (imageTask.Result.HasValue)
            {
                GameObject avatar = GameObject.CreatePrimitive(PrimitiveType.Quad);
                UnityEngine.Object.Destroy(avatar.GetComponent<Collider>());
                var avatarMr = avatar.GetComponent<MeshRenderer>();
                var avatarMat = avatarMr.material;
                avatarMat.shader = Shader.Find("Unlit/Texture");

                var avatarIcon = imageTask.Result.Value;

                Texture2D returnTexture = new Texture2D((int)avatarIcon.Width, (int)avatarIcon.Height, TextureFormat.RGBA32, false, true);
                GCHandle pinnedArray = GCHandle.Alloc(avatarIcon.Data, GCHandleType.Pinned);
                IntPtr pointer = pinnedArray.AddrOfPinnedObject();
                returnTexture.LoadRawTextureData(pointer, avatarIcon.Data.Length);
                returnTexture.Apply();
                pinnedArray.Free();

                avatarMat.mainTexture = returnTexture;

                avatar.transform.SetParent(namePlate.transform);
                avatar.transform.localScale = new Vector3(0.25f, -0.25f, 0.25f);
                avatar.transform.localPosition = new Vector3(0.0f, 0.2f, 0.0f);
            }
        }

        // Updates the NamePlate's direction to face towards the player's camera
        public void UpdateNameplateFacing(Transform cameraTransform)
        {
            if (namePlate.activeInHierarchy != ClientSettings.hiddenNametags)
                namePlate.SetActive(ClientSettings.hiddenNametags);

            if (namePlate.activeInHierarchy)
            {
                namePlate.transform.position = head.transform.position + (Vector3.up * 0.3f);
                namePlate.transform.rotation = cameraTransform.rotation;
            }
        }

        // Destroys the GameObjects stored inside this class, preparing this instance for deletion
        public void Delete()
        {
            Destroy(ford);
            Destroy(head);
            Destroy(handL);
            Destroy(handR);
            Destroy(pelvis);
            Destroy(namePlate);
        }

        // Applies the information recieved from the Transform packet
        public void ApplyTransformMessage(RigTransforms tfMsg)
        {
            BWUtil.ApplyRigTransform(rigTransforms, tfMsg);
        }
    }
}
