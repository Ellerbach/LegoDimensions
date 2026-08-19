// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

using LegoDimensions;
using LegoDimensions.Portal;
using LegoDimensionsRunner.Actions;
using System.Diagnostics;
using System.Text.Json;

namespace LegoDimensionsRunner
{
    public static class ProcessRunner
    {
        private static ILegoPortal[]? _portals;
        private static Runner? _runner;

        private static ILegoPortal[] Portals => _portals
            ?? throw new InvalidOperationException("Portals must be initialized before building a runner.");

        public static void CreateAllPortals()
        {
            var portals = LegoPortal.GetPortals();
            _portals = new ILegoPortal[portals.Length];
            for (int i = 0; i < portals.Length; i++)
            {
                _portals[i] = new LegoPortal(portals[i], i);
            }
        }

        public static void SetSinglePortal(ILegoPortal portal) => _portals = new ILegoPortal[] { portal };

        public static ILegoPortal[]? GetLegoPortals() => _portals;

        public static void Build(Runner runner)
        {
            foreach (var anim in runner.Animations)
            {
                var portalId = anim.PortalId;
                // Sanity check
                if ((portalId != null) && (portalId.Value < 0 || portalId.Value >= Portals.Length))
                {
                    continue;
                }

                anim.CompiledActions = new List<Action>();
                foreach (var action in anim.Actions)
                {
                    if (TryGetObject<SetColor>(action, out SetColor setColor))
                    {
                        anim.CompiledActions.Add(() =>
                        {
                            if (portalId == null)
                            {
                                for (int i = 0; i < Portals.Length; i++)
                                {
                                    Portals[i].SetColor(setColor.Pad, GetColorFromString(setColor.Color));
                                }
                            }
                            else
                            {
                                Portals[portalId.Value].SetColor(setColor.Pad, GetColorFromString(setColor.Color));
                            }

                            Thread.Sleep(setColor.Duration ?? 0);
                        });
                    }
                    else if (TryGetObject<SetColorAll>(action, out SetColorAll setColorAll))
                    {
                        anim.CompiledActions.Add(() =>
                        {
                            if (portalId == null)
                            {
                                for (int i = 0; i < Portals.Length; i++)
                                {
                                    Portals[i].SetColorAll(GetColorFromString(setColorAll.Center), GetColorFromString(setColorAll.Left), GetColorFromString(setColorAll.Right));
                                }
                            }
                            else
                            {
                                Portals[portalId.Value].SetColorAll(GetColorFromString(setColorAll.Center), GetColorFromString(setColorAll.Left), GetColorFromString(setColorAll.Right));
                            }

                            Thread.Sleep(setColorAll.Duration ?? 0);
                        });
                    }
                    else if (TryGetObject<Flash>(action, out Flash flash))
                    {
                        anim.CompiledActions.Add(() =>
                        {
                            if (portalId == null)
                            {
                                for (int i = 0; i < Portals.Length; i++)
                                {
                                    Portals[i].Flash(flash.Pad, new FlashPad(flash.TickOn, flash.TickOff, flash.TickCount, GetColorFromString(flash.Color), flash.Enabled));
                                }
                            }
                            else
                            {
                                Portals[portalId.Value].Flash(flash.Pad, new FlashPad(flash.TickOn, flash.TickOff, flash.TickCount, GetColorFromString(flash.Color), flash.Enabled));
                            }

                            Thread.Sleep(flash?.Duration ?? 0);
                        });
                    }
                    else if (TryGetObject<SwitchOffAll>(action, out SwitchOffAll switchOffAll))
                    {
                        anim.CompiledActions.Add(() =>
                        {
                            if (portalId == null)
                            {
                                for (int i = 0; i < Portals.Length; i++)
                                {
                                    Portals[i].SwitchOffAll();
                                }
                            }
                            else
                            {
                                Portals[portalId.Value].SwitchOffAll();
                            }

                            Thread.Sleep(switchOffAll?.Duration ?? 0);
                        });
                    }
                    else if (TryGetObject<FlashAll>(action, out FlashAll flashAll))
                    {
                        anim.CompiledActions.Add(() =>
                        {
                            if (portalId == null)
                            {
                                for (int i = 0; i < Portals.Length; i++)
                                {
                                    Portals[i].FlashAll(new FlashPad(flashAll.CenterTickOn, flashAll.CenterTickOn, flashAll.CenterTickOn, GetColorFromString(flashAll.CenterColor), flashAll.CenterEnabled),
                                        new FlashPad(flashAll.LeftTickOn, flashAll.LeftTickOff, flashAll.LeftTickCount, GetColorFromString(flashAll.LeftColor), flashAll.LeftEnabled),
                                        new FlashPad(flashAll.RightTickOn, flashAll.RightTickOff, flashAll.RightTickCount, GetColorFromString(flashAll.RightColor), flashAll.RightEnabled));
                                }
                            }
                            else
                            {
                                Portals[portalId.Value].FlashAll(new FlashPad(flashAll.CenterTickOn, flashAll.CenterTickOn, flashAll.CenterTickOn, GetColorFromString(flashAll.CenterColor), flashAll.CenterEnabled),
                                        new FlashPad(flashAll.LeftTickOn, flashAll.LeftTickOff, flashAll.LeftTickCount, GetColorFromString(flashAll.LeftColor), flashAll.LeftEnabled),
                                        new FlashPad(flashAll.RightTickOn, flashAll.RightTickOff, flashAll.RightTickCount, GetColorFromString(flashAll.RightColor), flashAll.RightEnabled));
                            }
                            Thread.Sleep(flashAll?.Duration ?? 0);
                        });
                    }
                    else if (TryGetObject<Fade>(action, out Fade fade))
                    {
                        anim.CompiledActions.Add(() =>
                        {
                            if (portalId == null)
                            {
                                for (int i = 0; i < Portals.Length; i++)
                                {
                                    Portals[i].Fade(fade.Pad, new FadePad(fade.TickTime, fade.TickCount, GetColorFromString(fade.Color), fade.Enabled));
                                }
                            }
                            else
                            {
                                Portals[portalId.Value].Fade(fade.Pad, new FadePad(fade.TickTime, fade.TickCount, GetColorFromString(fade.Color), fade.Enabled));
                            }

                            Thread.Sleep(fade?.Duration ?? 0);
                        });
                    }
                    else if (TryGetObject<FadeAll>(action, out FadeAll fadeAll))
                    {
                        anim.CompiledActions.Add(() =>
                        {
                            if (portalId == null)
                            {
                                for (int i = 0; i < Portals.Length; i++)
                                {
                                    Portals[i].FadeAll(new FadePad(fadeAll.CenterTickTime, fadeAll.CenterTickCount, GetColorFromString(fadeAll.CenterColor), fadeAll.CenterEnabled),
                                        new FadePad(fadeAll.LeftTickTime, fadeAll.LeftTickCount, GetColorFromString(fadeAll.LeftColor), fadeAll.LeftEnabled),
                                        new FadePad(fadeAll.RightTickTime, fadeAll.RightTickCount, GetColorFromString(fadeAll.RightColor), fadeAll.RightEnabled));
                                }
                            }
                            else
                            {
                                Portals[portalId.Value].FadeAll(new FadePad(fadeAll.CenterTickTime, fadeAll.CenterTickCount, GetColorFromString(fadeAll.CenterColor), fadeAll.CenterEnabled),
                                    new FadePad(fadeAll.LeftTickTime, fadeAll.LeftTickCount, GetColorFromString(fadeAll.LeftColor), fadeAll.LeftEnabled),
                                    new FadePad(fadeAll.RightTickTime, fadeAll.RightTickCount, GetColorFromString(fadeAll.RightColor), fadeAll.RightEnabled));
                            }

                            Thread.Sleep(fadeAll?.Duration ?? 0);
                        });
                    }
                    else if (TryGetObject<FadeRandom>(action, out FadeRandom fadeRandom))
                    {
                        anim.CompiledActions.Add(() =>
                        {
                            if (portalId == null)
                            {
                                for (int i = 0; i < Portals.Length; i++)
                                {
                                    Portals[i].FadeRandom(fadeRandom.Pad, fadeRandom.TickTime, fadeRandom.TickCount);
                                }
                            }
                            else
                            {
                                Portals[portalId.Value].FadeRandom(fadeRandom.Pad, fadeRandom.TickTime, fadeRandom.TickCount);
                            }

                            Thread.Sleep(fadeRandom?.Duration ?? 0);
                        });
                    }
                }
            }

            _runner = runner;
            for (int i = 0; i < Portals.Length; i++)
            {
                Portals[i].LegoTagEvent += PortalLegoTagEvent;
            }
        }

        private static void PortalLegoTagEvent(object? sender, LegoTagEventArgs e)
        {
            // First check by ID
            var id = _runner?.Events?.Where(m => (m.Id != null) && (m.Id == e.LegoTag?.Id)).Where(m => m.Pad == Pad.All || m.Pad == e.Pad);
            if (id != null && id.Any())
            {
                RunEvent(id);
            }
            else
            {
                var cardid = _runner?.Events?.Where(m => (m.CardId != null) && (m.CardId == BitConverter.ToString(e.CardUid))).Where(m => m.Pad == Pad.All || m.Pad == e.Pad);
                if (cardid != null && cardid.Any())
                {
                    RunEvent(cardid);
                }
                else
                {
                    // Check if we have a specific pad 
                    var pads = _runner?.Events?.Where(m => (m.Pad == Pad.All || m.Pad == e.Pad) && (m.Id == null && string.IsNullOrEmpty(m.CardId)));
                    if (pads != null && pads.Any())
                    {
                        RunEvent(pads);
                    }
                }
            }
        }

        private static void RunEvent(IEnumerable<Event> events)
        {
            foreach (var item in events)
            {
                var anims = _runner?.Animations.Where(m => m.Name == item.Animation);
                if (anims != null)
                {
                    foreach (var anim in anims)
                    {
                        foreach (var act in anim.CompiledActions)
                        {
                            act.Invoke();
                        }
                    }
                }
            }
        }

        public static Runner Deserialize(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<Runner>(json, options)
                ?? throw new JsonException("The runner configuration cannot be null.");
        }

        public static void Run(CancellationToken token)
        {
            if (_runner == null)
            {
                throw new InvalidOperationException("Build must be called before Run.");
            }

            while (!token.IsCancellationRequested)
            {
                foreach (var item in _runner.Playlist)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    // Play the animation
                    var anim = _runner.Animations.Where(m => string.Compare(m.Name, item, true) == 0).FirstOrDefault();
                    if (anim != null)
                    {
                        foreach (var act in anim.CompiledActions)
                        {
                            act.Invoke();
                        }
                    }
                }
            }
        }

        private static bool TryGetObject<T>(dynamic? ser, out T? action) where T : class
        {
            action = null;
            // First, check if we have a json object or just a string

            var actionRef = Activator.CreateInstance<T>();

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // And then use the normal json deserializer
                action = JsonSerializer.Deserialize<T>(ser, options);

            }
            catch
            {
                // Nothing on purpose
            }

            if (action != null)
            {
                var actionRefMethod = actionRef.GetType().GetMethods().Where(m => m.Name == "get_Name").FirstOrDefault();
                var actionMethod = action.GetType().GetMethods().Where(m => m.Name == "get_Name").FirstOrDefault();

                if (actionRefMethod?.Invoke(actionRef, null) == actionMethod?.Invoke(action, null))
                {
                    return true;
                }

                action = null;
            }

            // If just a string, then we have a key value list like key1=value1,key2=value2
            string? serialized;
            try
            {
                serialized = ser?.GetString();
            }
            catch
            {
                serialized = ser as string;
            }

            if (string.IsNullOrWhiteSpace(serialized))
            {
                return false;
            }

            var data = serialized.Trim().Split(',');

            Dictionary<string, string> dico = new Dictionary<string, string>();
            foreach (string s in data)
            {
                var dic = s.Trim().Split('=');
                dico.Add(dic[0].Trim().ToLower(), dic[1].Trim());
            }

            if (string.Compare(dico["name"], typeof(T).Name, true) == 0)
            {
                try
                {
                    var methods = typeof(T).GetMethods();
                    action = Activator.CreateInstance<T>();
                    foreach (var method in methods.Where(m => m.Name.StartsWith("set_")))
                    {
                        var mem = method.Name.ToLower();

                        if (dico.ContainsKey(mem.Substring(4)))
                        {
                            var toParse = dico[mem.Substring(4)];
                            var parameters = method.GetParameters();
                            object? param = null;
                            if (parameters[0].ParameterType == typeof(string))
                            {
                                param = toParse;
                            }
                            else if (parameters[0].ParameterType == typeof(int?))
                            {
                                if (!string.IsNullOrEmpty(toParse))
                                {
                                    var val = int.Parse(toParse);
                                    param = val;
                                }
                            }
                            else if (parameters[0].ParameterType == typeof(int))
                            {
                                if (!string.IsNullOrEmpty(toParse))
                                {
                                    var val = int.Parse(toParse);
                                    param = val;
                                }
                                else
                                {
                                    param = (int)0;
                                }
                            }
                            else if (parameters[0].ParameterType == typeof(bool))
                            {
                                if (!string.IsNullOrEmpty(toParse))
                                {
                                    var val = bool.Parse(toParse);
                                    param = val;
                                }
                            }
                            else if (parameters[0].ParameterType == typeof(Pad))
                            {
                                Enum.TryParse(toParse, out Pad val);
                                param = val;
                            }
                            else if (parameters[0].ParameterType == typeof(byte))
                            {
                                if (!string.IsNullOrEmpty(toParse))
                                {
                                    var val = byte.Parse(toParse);
                                    param = val;
                                }
                                else
                                {
                                    param = (byte)0;
                                }
                            }

                            method.Invoke(action, new[] { param });
                        }

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{ex}");
                }
            }

            return action != null;
        }

        private static Color GetColorFromString(string color)
        {
            // Default will be black but that won't thriw
            Color.TryGetColor(color, out var result);
            return result;
        }
    }
}
