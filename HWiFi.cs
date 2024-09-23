using Hiro.Plugin.Modelviews;
using Hiro.Plugin.Services;
using Windows.Devices.Radios;
using Windows.Devices.WiFi;

namespace Hiro.Plugin.Official.WiFi
{
    internal class HWiFi : IHiroPlugin
    {
        IHiroService? _service = null;
        string? IHiroPlugin.Id => "Hiro.WiFi";

        string? IHiroPlugin.Author => "Hiro";

        string? IHiroPlugin.Icon => @"\assets\icon.hif";

        HiroVersion? IHiroPlugin.Version => new HiroVersion()
        {
            Version = "1.0.0.0",
            CompareVersion = 1000000
        };

        bool? IHiroPlugin.RunWithHiro => false;

        bool? IHiroPlugin.IsolateRun => true;

        bool? IHiroPlugin.Dispose()
        {
            return true;
        }

        void IHiroPlugin.FirstRun()
        {
            _service?.Link("wifi:", "%1");
        }

        string? IHiroPlugin.GetDescription(string language)
        {
            switch (language.ToLower())
            {
                case "fr":
                    {
                        return "Adaptateur WiFi d'Hiro";
                    }
                case "ja-jp":
                    {
                        return "Hiro WiFi アダプター";
                    }
                case "zh-cn":
                    {
                        return "Hiro WiFi 控制器";
                    }
                default:
                    {
                        return "Hiro WiFi Adapter";
                    }

            }
        }

        string? IHiroPlugin.GetName(string language)
        {
            switch (language.ToLower())
            {
                case "fr":
                    {
                        return "WiFi";
                    }
                case "ja-jp":
                    {
                        return "WiFi";
                    }
                case "zh-cn":
                    {
                        return "WiFi";
                    }
                default:
                    {
                        return "WiFi";
                    }

            }
        }

        string? IHiroPlugin.HiroWeGo(string input, List<object>? para)
        {
            int situation = input.ToLower() switch
            {
                "wifi(0)" or "wifi(off)" => 0,
                "wifi(1)" or "wifi(on)" => 1,
                "wifi(2)" or "wifi(dis)" or "wifi(disconnect)" => 2,
                "wifi(3)" or "wifi(con)" or "wifi(connect)" => 3,
                _ => -1,
            };
            if (situation == -1 && para != null)
            {
                if (para.Count > 1 && (para[1].ToString() ?? string.Empty).ToLower().IndexOf("o") != -1)
                {
                    SetWiFiState(3, para[0].ToString(), true);
                }
                else
                    SetWiFiState(3, para[0].ToString());

            }
            else
                SetWiFiState(situation);
            return "success";
        }

        private async void SetWiFiState(int? WiFiState, string? Ssid = null, bool omit = false)
        {
            var lang = HWiFiHelper.getLang(_service);
            try
            {
                if (await WiFiAdapter.RequestAccessAsync() != WiFiAccessStatus.Allowed)
                {
                    _service?.Notify(new Hiro_Notice(HWiFiHelper.Get_Translate(lang, "wifi") + HWiFiHelper.Get_Translate(lang, "dcreject"), 2, HWiFiHelper.Get_Translate(lang, "wifi")));
                    return;
                }
                var adapters = await WiFiAdapter.FindAllAdaptersAsync();
                if (adapters.Count <= 0)
                {
                    _service?.Notify(new Hiro_Notice(HWiFiHelper.Get_Translate(lang, "wifi") + HWiFiHelper.Get_Translate(lang, "dcnull"), 2, HWiFiHelper.Get_Translate(lang, "wifi")));
                    return;
                }
                var adapter = adapters[0];
                if (null == adapter)
                {
                    _service?.Notify(new Hiro_Notice(HWiFiHelper.Get_Translate(lang, "wifi") + HWiFiHelper.Get_Translate(lang, "dcnull"), 2, HWiFiHelper.Get_Translate(lang, "wifi")));
                    return;
                }
                Radio? ra = null;
                foreach (var radio in await Radio.GetRadiosAsync())
                {
                    if (radio.Kind == RadioKind.WiFi)
                    {
                        ra = radio;
                        break;
                    }
                }
                if (null == ra)
                {
                    _service?.Notify(new Hiro_Notice(HWiFiHelper.Get_Translate(lang, "wifi") + HWiFiHelper.Get_Translate(lang, "dcnull"), 2, HWiFiHelper.Get_Translate(lang, "wifi")));
                    return;
                }
                switch (WiFiState)
                {
                    case 0:
                        await ra.SetStateAsync(RadioState.Off);
                        _service?.Notify(new Hiro_Notice(HWiFiHelper.Get_Translate(lang, "wifi") + HWiFiHelper.Get_Translate(lang, "dcoff"), 2, HWiFiHelper.Get_Translate(lang, "wifi")));
                        break;
                    case 1:
                        await ra.SetStateAsync(RadioState.On);
                        _service?.Notify(new Hiro_Notice(HWiFiHelper.Get_Translate(lang, "wifi") + HWiFiHelper.Get_Translate(lang, "dcon"), 2, HWiFiHelper.Get_Translate(lang, "wifi")));
                        await adapter.ScanAsync();
                        break;
                    case 2:
                        adapter.Disconnect();
                        _service?.Notify(new Hiro_Notice(HWiFiHelper.Get_Translate(lang, "wifi") + HWiFiHelper.Get_Translate(lang, "dcdiscon"), 2, HWiFiHelper.Get_Translate(lang, "wifi")));
                        break;
                    case 3:
                        await adapter.ScanAsync();
                        if (adapter.NetworkReport.AvailableNetworks.Count > 0)
                        {
                            if (isDebug())
                                _service?.Log($"adapter.NetworkReport.AvailableNetworks.Count {adapter.NetworkReport.AvailableNetworks.Count}");
                            var connect = true;
                            WiFiAvailableNetwork? savedan = null;
                            foreach (var an in adapter.NetworkReport.AvailableNetworks)
                            {
                                if (Ssid != null && an.Ssid.Equals(Ssid))
                                {
                                    if (savedan == null || !savedan.Ssid.Equals(Ssid))
                                    {
                                        if (isDebug())
                                            _service?.Log($"Matched Wifi Detected {an.Ssid}");
                                        savedan = an;
                                        if (omit)
                                            break;
                                    }
                                    else
                                    {
                                        if (isDebug())
                                            _service?.Log($"Multi Wifi Detected {an.Ssid}");
                                        connect = false;
                                        break;
                                    }
                                }
                                else if (an.SecuritySettings.NetworkAuthenticationType.ToString().ToLower().StartsWith("open") && savedan == null)
                                {
                                    if (isDebug())
                                        _service?.Log($"Open Wifi Detected {an.Ssid}");
                                    savedan = an;
                                    break;
                                }
                            }
                            if (!connect)
                                _service?.Notify(new Hiro_Notice(HWiFiHelper.Get_Translate(lang, "wifimis").Replace("%s", Ssid), 2, HWiFiHelper.Get_Translate(lang, "wifi")));
                            else
                            {
                                if (savedan == null)
                                    _service?.Notify(new Hiro_Notice(HWiFiHelper.Get_Translate(lang, "wifina").Replace("%s", Ssid), 2, HWiFiHelper.Get_Translate(lang, "wifi")));
                                else
                                {
                                    await adapter.ConnectAsync(savedan, Windows.Devices.WiFi.WiFiReconnectionKind.Automatic);
                                    if (Ssid != null && !savedan.Ssid.ToLower().Equals(Ssid.ToLower()))
                                        _service?.Notify(new Hiro_Notice(HWiFiHelper.Get_Translate(lang, "wifi") + HWiFiHelper.Get_Translate(lang, "dcrecon").Replace("%s1", Ssid).Replace("%s2", savedan.Ssid), 2, HWiFiHelper.Get_Translate(lang, "wifi")));
                                    else
                                        _service?.Notify(new Hiro_Notice(HWiFiHelper.Get_Translate(lang, "wifi") + HWiFiHelper.Get_Translate(lang, "dccon").Replace("%s", savedan.Ssid), 2, HWiFiHelper.Get_Translate(lang, "wifi")));
                                }
                            }
                        }
                        else
                            _service?.Notify(new Hiro_Notice(HWiFiHelper.Get_Translate(lang, "wifina"), 2, HWiFiHelper.Get_Translate(lang, "wifi")));
                        break;
                    default:
                        _service?.Notify(new Hiro_Notice(HWiFiHelper.Get_Translate(lang, "syntax"), 2, HWiFiHelper.Get_Translate(lang, "wifi")));
                        break;
                }
            }
            catch (Exception ex)
            {
                _service?.Notify(new Hiro_Notice(HWiFiHelper.Get_Translate(lang, "error"), 2, HWiFiHelper.Get_Translate(lang, "wifi")));
                _service?.LogError(ex, $"Hiro.Exception.Wifi");
            }
        }

        bool isDebug()
        {
            var _list = _service?.GetData("Hiro.DFlag", null);
            if (_list == null || _list.Count == 0)
                return false;
            return _list[0] as bool? ?? false;
        }

        bool? IHiroPlugin.HiroWndProc(int procID, List<object>? para)
        {
            return null;
        }

        void IHiroPlugin.Initialize(IHiroService hostService)
        {
            _service = hostService;
        }

        void IHiroPlugin.UpdatedRun()
        {

        }
    }
}
