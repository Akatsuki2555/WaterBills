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
        public override string Name => "Water Bills";
        public override string ID => "waterbills";
        public override string Version => "1.0.1";
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

        private SettingsSlider _costPerLiter;
        private SettingsSlider _combustionTap;
        private SettingsSlider _combustionShower;

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

            _costPerLiter = Settings.AddSlider(this, "costperl", "Cost Per Litre", 5f, 15, 11);
            Settings.AddText(this,
                "<color=red>WARNING: Changing the value above is not possible after the game is loaded.</color>");

            _combustionTap = Settings.AddSlider(this, "combustiontap", "Combustion Tap Multiplier", 1, 10, 1f);
            _combustionShower = Settings.AddSlider(this, "combustionshower", "Combustion Shower Multiplier", 1, 10, 2f);

            Settings.AddButton(this, "Author Socials", () =>
            {
                Application.OpenURL("https://akatsuki.nekoweb.org/");
            });

            Settings.AddButton(this, "Join my Discord server", () =>
            {
                ModUI.ShowYesNoMessage(
                    "This Discord server is LGBTQ+ inclusive and has strict rules. If you are not happy with that, kindly press no.",
                    () =>
                    {
                        Application.OpenURL("https://akatsuki.nekoweb.org/discord");
                    });
            });
        }

        private readonly FsmBool _waterBillsPaid = new FsmBool("WaterBillsPaid")
        {
            Value = true
        };

        private readonly FsmFloat _timer = new FsmFloat("WaterCutTimer")
        {
            Value = 50400f
        };

        private readonly FsmFloat _cost = new FsmFloat("Cost")
        {
            Value = 11
        };

        private readonly FsmFloat _litres = new FsmFloat("Litres")
        {
            Value = 0
        };

        private readonly FsmFloat _litresCalc = new FsmFloat("LitresCalc")
        {
            Value = 0
        };

        private GameObject _envelope;
        private FsmBool _kitchenTap;
        private FsmBool _bathroomTap, _bathroomShower;

        private void Mod_Load()
        {
            if (!SaveLoad.ValueExists(this, "WaterCost"))
                SaveLoad.WriteValue(this, "WaterCost", _costPerLiter.GetValue());

            var waterCost = SaveLoad.ReadValue<float>(this, "WaterCost");
            _cost.Value = waterCost;

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

            payFsmPay.InsertAction(3, new SetFloatValue()
            {
                floatVariable = _litres,
                floatValue = 0
            });

            waterBill.SetActive(false);
            _envelope = Object.Instantiate(GameObject.Find("YARD").C(4).C(1));
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
            _timer.Value -= Time.deltaTime * Mathf.Max(1, _litresCalc.Value / 1000);
            _waterBillsPaid.Value = _timer.Value > 0;
            _envelope.SetActive(_timer.Value < 21600);

            if (_kitchenTap.Value) _litres.Value += (Time.deltaTime / 5) * _combustionTap.GetValue();
            if (_bathroomShower.Value) _litres.Value += (Time.deltaTime / 5) * _combustionShower.GetValue();
            if (_bathroomTap.Value) _litres.Value += (Time.deltaTime / 5) * _combustionTap.GetValue();

            _litresCalc.Value = _litres.Value * _cost.Value;
        }
    }
}