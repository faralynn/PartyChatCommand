using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing.Design;
using System.IO;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Plugin;
using wManager.Wow.Class;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class Main : IPlugin
{
    private bool _isLaunched;

    public void Initialize()
    {
        _isLaunched = true;
        PartyChatCommandSettings.Load();
        Logging.Write("[PartyChatCommand] Started.");

        var chat = new Channel();

        EventsLuaWithArgs.OnEventsLuaWithArgs += EventsLuaWithArgsOnOnEventsLuaWithArgs;

        while (_isLaunched && Products.IsStarted)
        {
            try
            {
                while (chat.ActuelRead != Channel.GetMsgActuelInWow && Products.IsStarted)
                {
                    //var msg = chat.ReadMsg();
                    //var sheeit = "wat";
                    //Lua.LuaDoString("print(\"" + msg + "\")");
                    ////Lua.LuaDoString("print(\"" + sheeit + "\")");
                    //if (!string.IsNullOrWhiteSpace(msg.Msg) &&
                    //    ((int)msg.Channel == 49 || msg.Channel == ChatTypeId.WHISPER))
                    //{

                    //    foreach (var c in PartyChatCommandSettings.CurrentSetting.Commands)
                    //    {
                    //        if (c.IsValid() && string.Equals(c.CommandChat.Trim(), msg.Msg.Trim(), StringComparison.CurrentCultureIgnoreCase))
                    //        {
                    //            c.Run();
                    //            break;
                    //        }
                    //    }
                    //}
                }
            }
            catch (Exception e)
            {
                Logging.WriteError("[PartyChatCommand]: " + e);
            }
            Thread.Sleep(150);
        }
    }

    public void Dispose()
    {
        EventsLuaWithArgs.OnEventsLuaWithArgs -= EventsLuaWithArgsOnOnEventsLuaWithArgs;
        _isLaunched = false;
        Logging.Write("[PartyChatCommand] Stoped.");
    }

    public void Settings()
    {
        PartyChatCommandSettings.Load();
        PartyChatCommandSettings.CurrentSetting.ToForm();
        PartyChatCommandSettings.CurrentSetting.Save();
        Logging.Write("[PartyChatCommand] Settings saved.");
    }

    private void EventsLuaWithArgsOnOnEventsLuaWithArgs(LuaEventsId id, List<string> args)
    {
        //Main.LuaPrint("PF: " + id.ToString() + " /" + args[0] + " /" + args[1] + " /" + args[2]);

        if (id.ToString() == "CHAT_MSG_ADDON")
        {


            //https://wrobot.eu/bugtracker/code-doesnt-stop-fighting-complete-r1236/

            string prefix = args[0];
            string message = args[1];
            string sender = args[3];

            //Main.LuaPrint("msg: " + prefix + message + sender);

            foreach (var c in PartyChatCommandSettings.CurrentSetting.Commands)
            {
                if (c.IsValid() && string.Equals(c.CommandChat.Trim(), message, StringComparison.CurrentCultureIgnoreCase))
                {
                    c.Run();
                    break;
                }
            }

        }


    }


    #region Logging
    public static void LuaPrint(string Text)
    {
        Lua.LuaDoString("print(\"" + Text + "\")");
    }
    #endregion Logging
}



public class PartyChatCommandSettings : Settings
{
    public PartyChatCommandSettings()
    {
        Commands = new Command[0];
    }

    public static PartyChatCommandSettings CurrentSetting { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("PartyChatCommand", ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("PartyChatCommandSettings > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("PartyChatCommand", ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting =
                    Load<PartyChatCommandSettings>(AdviserFilePathAndName("PartyChatCommand",
                                                                 ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            else
            {
                CurrentSetting = new PartyChatCommandSettings
                {
                    Commands = new[]
                    {
                        new Command
                        {
                            CommandChat = "gohome",
                            CommandAction = "6948",
                            Type = CommandType.UseItem
                        },
                        new Command
                        {
                            CommandChat = "stay",
                            CommandAction = "robotManager.Products.Products.InPause = true; wManager.Wow.Helpers.Fight.StopFight(); wManager.Wow.Helpers.MovementManager.StopMove();",
                            Type = CommandType.CSharp
                        },
                        new Command
                        {
                            CommandChat = "follow",
                            CommandAction = "robotManager.Products.Products.InPause = false;",
                            Type = CommandType.CSharp
                        },
                        new Command
                        {
                            CommandChat = "stop",
                            CommandAction = "robotManager.Products.Products.ProductStop();",
                            Type = CommandType.CSharp
                        },
                    }
                };
            }
        }
        catch (Exception e)
        {
            Logging.WriteError("PartyChatCommandSettings > Load(): " + e);
        }
        return false;
    }

    [Setting]
    [Category("Settings")]
    [DisplayName("Command")]
    [Description("Put command here")]
    public Command[] Commands { get; set; }

    [Serializable]
    public class Command
    {
        public Command()
        {
            CommandChat = string.Empty;
            CommandAction = string.Empty;
            Type = CommandType.Lua;
        }

        [Setting]
        [DisplayName("Command")]
        [Description("Put here command received in chat (party channel)")]
        public string CommandChat { get; set; }

        [Setting]
        [DisplayName("Command action")]
        [Description("Put here action triggered by the command")]
        [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
        public string CommandAction { get; set; }

        [Setting]
        [DisplayName("Action command type")]
        [Description("Select action type")]
        public CommandType Type { get; set; }

        public bool IsValid()
        {
            try
            {
                return !string.IsNullOrWhiteSpace(CommandChat) && !string.IsNullOrWhiteSpace(CommandAction);
            }
            catch
            {
                return false;
            }
        }

        public void Run()
        {
            try
            {
                Logging.Write("[PartyChatCommand] Run " + ToString());
                switch (Type)
                {
                    case CommandType.Lua:
                        Lua.LuaDoString(CommandAction);
                        break;
                    case CommandType.LuaMacro:
                        Lua.RunMacroText(CommandAction);
                        break;
                    case CommandType.UseItem:
                        var n = Others.ParseUInt(CommandAction);
                        if (n > 0)
                            ItemsManager.UseItem(n);
                        else
                            ItemsManager.UseItem(CommandAction);
                        break;
                    case CommandType.CastSpell:
                        var s = new Spell(CommandAction);
                        s.Launch();
                        break;
                    case CommandType.CSharp:
                        RunCodeExtension.RunCsharpScript(CommandAction);
                        break;
                    case CommandType.LuaBot:
                        RunCodeExtension.RunLuaBotScript(CommandAction);
                        break;
                    case CommandType.VB:
                        RunCodeExtension.RunVBScript(CommandAction);
                        break;
                }
            }
            catch
            {
            }
        }

        public override string ToString()
        {
            try
            {
                return "[" + Type + "] " + CommandChat + " > " + CommandAction;
            }
            catch (Exception)
            {
                return base.ToString();
            }
        }
    }

    public enum CommandType
    {
        Lua,
        LuaMacro,
        UseItem,
        CastSpell,
        CSharp,
        LuaBot,
        VB
    }

}
