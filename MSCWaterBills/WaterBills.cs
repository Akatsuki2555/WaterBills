using System;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using MSCLoader;
using UnityEngine;
using Object = UnityEngine.Object;

// Below turning off one inspection as it is not available for Unity 5.0.0f4
// ReSharper disable Unity.InstantiateWithoutParent

namespace MSCWaterBills
{
    public class WaterBills : Mod
    {
        public override string ID => "waterbills";
        public override string Version => "1.0";
        public override string Author => "アカツキ";

        public override void ModSetup()
        {
            base.ModSetup();

            SetupFunction(Setup.OnLoad, Mod_Load);
            SetupFunction(Setup.Update, Mod_Update);
            SetupFunction(Setup.OnSave, Mod_Save);
        }

        private void Mod_Save()
        {
            SaveLoad.WriteValue(this, "WaterCutTimer", _timer.Value);
            SaveLoad.WriteValue(this, "WaterLitres", _litres.Value);
        }


        public override void ModSettings()
        {
            base.ModSettings();

#if DEBUG

            Settings.AddButton(this, "DEBUG Set WaterBillsPaid True", () => { _waterBillsPaid.Value = true; });
            Settings.AddButton(this, "DEBUG Set WaterBillsPaid False", () => { _waterBillsPaid.Value = false; });
            Settings.AddButton(this, "DEBUG Set timer to 10", () => { _timer.Value = 10; });
            Settings.AddButton(this, "DEBUG Set timer to 50400", () => { _timer.Value = 50400; });
            Settings.AddButton(this, "DEBUG Add 1000L", () => { _litres.Value += 1000; });
            Settings.AddButton(this, "DEBUG Add 100L", () => { _litres.Value += 100; });

#endif
        }

        private FsmBool _waterBillsPaid = new FsmBool("WaterBillsPaid")
        {
            Value = true
        };

        private FsmFloat _timer = new FsmFloat("WaterCutTimer")
        {
            Value = 50400f
        };

        private FsmFloat _cost = new FsmFloat("Cost")
        {
            Value = 11
        };

        private FsmFloat _litres = new FsmFloat("Litres")
        {
            Value = 0
        };

        private FsmFloat _litresCalc = new FsmFloat("LitresCalc")
        {
            Value = 0
        };

        private GameObject _envelope;
        private FsmBool _kitchenTap;
        private FsmBool _bathroomTap, _bathroomShower;

        private void Mod_Load()
        {
            var go = new GameObject("WaterBills");
            var fsm = go.AddComponent<PlayMakerFSM>();
            fsm.FsmName = "WaterBillsMgr";

            fsm.AddVariable(_waterBillsPaid);
            fsm.AddVariable(_timer);
            fsm.AddVariable(_cost);
            fsm.AddVariable(_litres);
            fsm.AddVariable(_litresCalc);

            if (SaveLoad.ValueExists(this, "WaterCutTimer"))
                _timer.Value = SaveLoad.ReadValue<float>(this, "WaterCutTimer");
            if (SaveLoad.ValueExists(this, "WaterLitres"))
                _litres.Value = SaveLoad.ReadValue<float>(this, "WaterLitres");

            ApplyWaterBillsKitchenTap();
            ApplyWaterBillsShower();

            var waterBill = Object.Instantiate(GameObject.Find("Sheets").C(15));
            waterBill.SetActive(true);
            waterBill.name = "WaterBill";
            waterBill.transform.parent = GameObject.Find("Sheets").transform;

            waterBill.C(4).C(1).GetComponent<TextMesh>().text = "L";
            waterBill.C(5).C(1).GetComponent<TextMesh>().text = "mk/L";

            var waterBillFsm = waterBill.GetPlayMaker("Data");

            var waterBillFsmUnpaidBills = waterBillFsm.GetState("Set data").GetAction<GetFsmFloat>(0);
            waterBillFsmUnpaidBills.gameObject.GameObject = go;
            waterBillFsmUnpaidBills.fsmName = "WaterBillsMgr";
            waterBillFsmUnpaidBills.variableName = "LitresCalc";
            waterBillFsmUnpaidBills.everyFrame = false;

            var waterBillFsmPrice = waterBillFsm.GetState("Set data").GetAction<GetFsmFloat>(1);
            waterBillFsmPrice.gameObject.GameObject = go;
            waterBillFsmPrice.fsmName = "WaterBillsMgr";
            waterBillFsmPrice.variableName = "Cost";

            var payFsm = waterBill.C(0).GetPlayMaker("Button");
            var payFsmCheckMoney = payFsm.GetState("Check money").GetAction<GetFsmFloat>(0);
            payFsmCheckMoney.gameObject.GameObject = go;
            payFsmCheckMoney.fsmName = "WaterBillsMgr";
            payFsmCheckMoney.variableName = "LitresCalc";

            var payFsmPay = payFsm.GetState("Date");

            payFsmPay.RemoveAction(2);
            payFsmPay.InsertAction(2, new SetFloatValue()
            {
                floatVariable = _timer,
                floatValue = 50400
            });

            waterBill.SetActive(false);
            _envelope = GameObject.Instantiate(GameObject.Find("YARD").C(4).C(1));
            _envelope.name = "EnvelopeWaterBill";
            _envelope.transform.parent = GameObject.Find("YARD").C(4).transform;
            _envelope.transform.localPosition = new Vector3(0.014f, -0.0026f, 0.1807f);
            _envelope.transform.localRotation = Quaternion.Euler(0, 0, 0);

            _envelope.GetPlayMaker("Use").enabled = true;
            _envelope.GetPlayMaker("Use").GetState("State 2").GetAction<SetStringValue>(1).stringValue =
                "WATER BILL";
            _envelope.GetPlayMaker("Use").GetState("Open bill").GetAction<ActivateGameObject>(1).gameObject
                .GameObject = waterBill;
        }

        private void ApplyWaterBillsKitchenTap()
        {
            var kitchenTap = GameObject.Find("YARD").C(2).C(6).C(3).C(2).GetPlayMaker("Use");
            var kitchenTapAllTrue = kitchenTap.GetState("Check elec").GetAction<BoolAllTrue>(1);
            kitchenTapAllTrue.boolVariables = kitchenTapAllTrue.boolVariables.AddToArray(_waterBillsPaid);
            _kitchenTap = kitchenTap.GetVariable<FsmBool>("SwitchOn");
        }

        private void ApplyWaterBillsShower()
        {
            var showerTap = GameObject.Find("YARD").C(2).C(4).C(1).C(4).GetPlayMaker("Switch");
            showerTap.GetState("Shower").GetAction<BoolAllTrue>(4).boolVariables.AddToArray(_waterBillsPaid);
            showerTap.GetState("Tap").GetAction<BoolAllTrue>(3).boolVariables.AddToArray(_waterBillsPaid);

            _bathroomShower = showerTap.GetVariable<FsmBool>("ShowerOn");
            _bathroomTap = showerTap.GetVariable<FsmBool>("TapOn");
        }

        private void Mod_Update()
        {
            _timer.Value -= Time.deltaTime * (_litres.Value / 1000);
            _waterBillsPaid.Value = _timer.Value > 0;
            _envelope.SetActive(_timer.Value < 21600);

            if (_kitchenTap.Value) _litres.Value += Time.deltaTime / 5;
            if (_bathroomShower.Value) _litres.Value += Time.deltaTime / 2;
            if (_bathroomTap.Value) _litres.Value += Time.deltaTime / 5;

            _litresCalc.Value = _litres.Value * _cost.Value;
        }
    }
}