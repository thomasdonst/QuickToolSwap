using System.Collections.Generic;
using System.Linq;
using CoreLib;
using CoreLib.Data.Configuration;
using CoreLib.RewiredExtension;
using PlayerCommand;
using PugMod;
using Rewired;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace QuickToolSwap
{
    public class QuickToolSwap : IMod
    {
        private const string Version = "1.1.0";
        private const string Author = "thomas1267";
        private const string Name = "QuickToolSwap";

        private const string KeyBindName = "QuickToolSwapKeyBind";
        private const string KeyBindDescription = "Swap tools quickly";

        private static int _equippedSlotIndex;
        private static int _toolIndex;

        private static bool _isToolSwapped;

        private static ConfigReader _configReader;

        public void EarlyInit()
        {
            Debug.Log($"{Name}: Version: {Version}, Author: {Author}");

            CoreLibMod.LoadModules(typeof(RewiredExtensionModule));
            RewiredExtensionModule.AddKeybind(KeyBindName, KeyBindDescription, KeyboardKeyCode.LeftShift);
            RewiredExtensionModule.SetDefaultControllerBinding(KeyBindName, GamepadTemplate.elementId_rightTrigger);

            var loadedMod = API.ModLoader.LoadedMods.FirstOrDefault(obj => obj.Handlers.Contains(this));
            var configFile = new ConfigFile($"{Name}/config.cfg", true, loadedMod);
            _configReader = new ConfigReader(configFile);
        }

        public void Init()
        {
        }

        public void ModObjectLoaded(Object obj)
        {
        }

        public void Shutdown()
        {
        }

        public void Update()
        {
            if (!IsPlayerEnabled()) return;

            HandleSwap();
            HandleSwapBack();
            LockEquipSlotOnSwap();
        }

        private static void HandleSwap()
        {
            if (!IsSwapAllowed()) return;

            // Retrieve all tiles and entities that the player is standing in front of and looking at.
            var targetPosition =
                (Manager.main.player.WorldPosition + Manager.main.player.targetingDirection).RoundToInt2();
            GetTilesAt(targetPosition, out var tiles);
            GetEntitiesAt(targetPosition, out var entities);

            // Choose the most suitable tool based on the identified tiles and entities.
            // Swap the selected tool with the currently equipped slot.
            _toolIndex = DetermineTool(tiles, entities);
            _equippedSlotIndex = Manager.main.player.equippedSlotIndex;
            _isToolSwapped = SwapObjects(_toolIndex, _equippedSlotIndex);
        }

        private static void HandleSwapBack()
        {
            if (!IsSwapBackAllowed()) return;

            SwapObjects(_toolIndex, _equippedSlotIndex, true);
            SetDefaultSwapState();
        }

        private static void LockEquipSlotOnSwap()
        {
            if (!IsEquipSlotLockable()) return;

            Manager.main.player.EquipSlot(_equippedSlotIndex);
        }

        private static void GetTilesAt(int2 position, out List<TileCD> tiles)
        {
            var clientSystem = API.Client.World.GetExistingSystemManaged<ClientSystem>();
            var tileAccessor = new TileAccessor(ref clientSystem.CheckedStateRef);
            var tileList = tileAccessor.Get(position, Allocator.Temp);
            tiles = tileList.ToList();
            tileList.Dispose();
        }

        private static void GetEntitiesAt(int2 position, out List<Entity> entities)
        {
            entities = new List<Entity>();
            var entityManager = API.Client.World.EntityManager;
            var queryDesc = new EntityQueryDesc
            {
                All = new[]
                    { ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadOnly<LocalTransform>() },
                None = new[] { ComponentType.ReadOnly<PlayerGhost>() }
            };
            var query = entityManager.CreateEntityQuery(queryDesc);
            var array = query.ToEntityArray(Allocator.Temp);
            foreach (var entity2 in array)
            {
                var transform = entityManager.GetComponentData<LocalTransform>(entity2);
                var actualPosition = transform.Position.RoundToInt2();
                if (position.x == actualPosition.x && position.y == actualPosition.y)
                {
                    entities.Add(entity2);
                }
            }

            array.Dispose();
        }

        private static int DetermineTool(List<TileCD> tiles, List<Entity> entities)
        {
            var tileTypes = tiles.Select(x => x.tileType.ToString()).ToList();
            var objectIds = entities.Select(x => GetObjectData(x).objectID.ToString()).ToList();
            var foundElements = tileTypes.Concat(objectIds).ToList();

            return GetIndex(_configReader.GetPriorityList(foundElements));
        }

        private static int GetIndex(List<string> priorityList)
        {
            var items = new Dictionary<string, int>();
            var inventorySize = Manager.main.player.playerInventoryHandler.size;
            for (var i = 0; i < inventorySize; i++)
            {
                var objectData = GetObjectData(i);
                var itemName = objectData.objectID.ToString().ToLower();

                if (objectData.amount == 0 || items.ContainsKey(itemName))
                    continue;

                items.Add(itemName, i);
            }

            foreach (var priority in priorityList)
                if (items.TryGetValue(priority, out var index))
                    return index;

            return items.GetValueOrDefault(ObjectID.Torch.ToString().ToLower(), -1);
        }

        private static bool SwapObjects(int toolIndex, int equippedSlotIndex, bool isSwapBack = false)
        {
            var playerController = Manager.main.player;
            var inventoryHandler = playerController.playerInventoryHandler;

            if (IsInventoryIndexOutOfRange(toolIndex) || IsInventoryIndexOutOfRange(equippedSlotIndex))
                return false;

            if (GetObjectInfo(toolIndex) == null && !isSwapBack)
                return false;

            inventoryHandler.Swap(playerController, toolIndex, inventoryHandler, equippedSlotIndex);

            return true;
        }

        private static ObjectData GetObjectData(Entity entity)
        {
            var world = API.Client.World;
            return EntityUtility.GetObjectData(entity, world);
        }

        private static ObjectData GetObjectData(int inventoryIndex)
        {
            var playerController = Manager.main.player;
            var inventoryHandler = playerController.playerInventoryHandler;
            return inventoryHandler.GetObjectData(inventoryIndex);
        }

        private static ObjectInfo GetObjectInfo(int inventoryIndex)
        {
            return PugDatabase.GetObjectInfo(GetObjectData(inventoryIndex).objectID);
        }

        private static void SetDefaultSwapState()
        {
            _toolIndex = -1;
            _equippedSlotIndex = -1;
            _isToolSwapped = false;
        }

        private static bool IsAnyUIOpen()
        {
            bool[] elements =
            {
                Manager.ui.isAnyInventoryShowing,
                Manager.ui.cookingCraftingUI.isShowing,
                Manager.ui.processResourcesCraftingUI.isShowing,
                Manager.ui.isSalvageAndRepairUIShowing,
                Manager.ui.bossStatueUI.isShowing,
                Manager.ui.isShowingMap,
                Manager.menu.IsAnyMenuActive()
            };

            return elements.Any(element => element);
        }

        private static bool IsPlayerFishing()
        {
            return Manager.main.player != null && Manager.main.player.fishingRodLine.gameObject.activeInHierarchy;
        }

        private static bool IsPlayerEnabled()
        {
            return Manager.main.player != null && Manager.main.player.enabled;
        }

        private static bool WasKeyBindPressedDownThisFrame()
        {
            var keyBindId = (PlayerInput.InputType)RewiredExtensionModule.GetKeybindId(KeyBindName);
            return Manager.input.singleplayerInputModule.WasButtonPressedDownThisFrame(keyBindId);
        }

        private static bool IsKeyBindCurrentlyDown()
        {
            var keyBindId = (PlayerInput.InputType)RewiredExtensionModule.GetKeybindId(KeyBindName);
            return Manager.input.singleplayerInputModule.IsButtonCurrentlyDown(keyBindId);
        }

        private static bool IsMouseButtonCurrentlyDown()
        {
            return Input.GetMouseButton(0) || Input.GetMouseButton(1);
        }

        private static bool IsSwapBackAllowed()
        {
            var isToolCurrentlyUsed = IsMouseButtonCurrentlyDown() || IsKeyBindCurrentlyDown() || IsPlayerFishing();
            var isKeyBindReleasedWhileToolUnused = !IsKeyBindCurrentlyDown() && !isToolCurrentlyUsed;
            return _isToolSwapped && (isKeyBindReleasedWhileToolUnused || IsAnyUIOpen());
        }

        private static bool IsSwapAllowed()
        {
            return !_isToolSwapped && !IsAnyUIOpen() && WasKeyBindPressedDownThisFrame();
        }

        private static bool IsEquipSlotLockable()
        {
            return Manager.main.player.equippedSlotIndex != _equippedSlotIndex && _isToolSwapped;
        }

        private static bool IsInventoryIndexOutOfRange(int index)
        {
            var playerController = Manager.main.player;
            var maxInventorySize = playerController.playerInventoryHandler.size - 1;
            return index < 0 || index > maxInventorySize;
        }
    }
}
