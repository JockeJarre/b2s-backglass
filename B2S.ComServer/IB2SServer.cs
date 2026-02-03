using System;
using System.Runtime.InteropServices;

namespace B2S.ComServer
{
    [ComVisible(true)]
    [Guid("5693c68c-5834-466d-aaac-a86922076efd")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IB2SServer
    {
        // Version and Directory Information
        [DispId(1)]
        string B2SServerVersion { get; }
        
        [DispId(2)]
        double B2SBuildVersion { get; }
        
        [DispId(3)]
        string B2SServerDirectory { get; }

        // VPinMAME Control and Properties
        [DispId(10)]
        string GameName { get; set; }
        
        [DispId(11)]
        string ROMName { get; }
        
        [DispId(12)]
        string B2SName { get; set; }
        
        [DispId(13)]
        string TableName { get; set; }
        
        [DispId(14)]
        string WorkingDir { set; }
        
        [DispId(15)]
        void SetPath(string path);
        
        [DispId(16)]
        object Games(string gamename);
        
        [DispId(17)]
        object Settings { get; }
        
        [DispId(18)]
        bool Running { get; }
        
        [DispId(19)]
        double TimeFence { set; }
        
        [DispId(20)]
        bool Pause { get; set; }
        
        [DispId(21)]
        string Version { get; }
        
        [DispId(22)]
        double PMBuildVersion { get; }
        
        [DispId(23)]
        void Run([Optional] object handle);
        
        [DispId(24)]
        void Stop();
        
        [DispId(25)]
        bool LaunchBackglass { get; set; }

        // Customization
        [DispId(30)]
        string SplashInfoLine { get; set; }
        
        [DispId(31)]
        bool ShowFrame { get; set; }
        
        [DispId(32)]
        bool ShowTitle { get; set; }
        
        [DispId(33)]
        bool ShowDMDOnly { get; set; }
        
        [DispId(34)]
        bool ShowPinDMD { get; set; }
        
        [DispId(35)]
        bool LockDisplay { get; set; }
        
        [DispId(36)]
        bool DoubleSize { get; set; }
        
        [DispId(37)]
        bool Hidden { get; set; }
        
        [DispId(38)]
        void SetDisplayPosition(object x, object y, object handle);
        
        [DispId(39)]
        void ShowOptsDialog(object handle);
        
        [DispId(40)]
        void ShowPathesDialog(object handle);
        
        [DispId(41)]
        void ShowAboutDialog(object handle);
        
        [DispId(42)]
        void CheckROMS(object showoptions, object handle);
        
        [DispId(43)]
        bool PuPHide { get; set; }

        // Game Settings
        [DispId(50)]
        bool HandleKeyboard { get; set; }
        
        [DispId(51)]
        short HandleMechanics { get; set; }

        // Polling Functions
        [DispId(60)]
        object ChangedLamps { get; }
        
        [DispId(61)]
        object ChangedSolenoids { get; }
        
        [DispId(62)]
        object ChangedGIStrings { get; }
        
        [DispId(63)]
        object ChangedLEDs(object mask2, object mask1, object mask3, object mask4);
        
        [DispId(64)]
        object NewSoundCommands { get; }

        // B2S Data Methods
        [DispId(100)]
        void B2SSetData(object idORname, object value);
        
        [DispId(101)]
        void B2SSetDataByName(object name, object value);
        
        [DispId(102)]
        void B2SSetFlash(object idORname);
        
        [DispId(103)]
        void B2SSetPos(object idORname, object xpos, object ypos);
        
        [DispId(104)]
        void B2SSetIllumination(object name, object value);
        
        [DispId(105)]
        void B2SSetLED(object digit, object valueORtext);
        
        [DispId(106)]
        void B2SSetLEDDisplay(object display, object text);
        
        [DispId(107)]
        void B2SSetReel(object digit, object value);
        
        [DispId(108)]
        void B2SSetScore(object display, object value);
        
        [DispId(109)]
        void B2SSetScorePlayer(object playerno, object score);
        
        [DispId(110)]
        void B2SSetScorePlayer1(object score);
        
        [DispId(111)]
        void B2SSetScorePlayer2(object score);
        
        [DispId(112)]
        void B2SSetScorePlayer3(object score);
        
        [DispId(113)]
        void B2SSetScorePlayer4(object score);
        
        [DispId(114)]
        void B2SSetScorePlayer5(object score);
        
        [DispId(115)]
        void B2SSetScorePlayer6(object score);
        
        [DispId(116)]
        void B2SSetScoreDigit(object digit, object value);
        
        [DispId(117)]
        void B2SSetScoreRollover(object id, object value);
        
        [DispId(118)]
        void B2SSetScoreRolloverPlayer1(object value);
        
        [DispId(119)]
        void B2SSetScoreRolloverPlayer2(object value);
        
        [DispId(120)]
        void B2SSetScoreRolloverPlayer3(object value);
        
        [DispId(121)]
        void B2SSetScoreRolloverPlayer4(object value);
        
        [DispId(122)]
        void B2SSetCredits(object digitORvalue, object value);
        
        [DispId(123)]
        void B2SSetPlayerUp(object idORvalue, object value);
        
        [DispId(124)]
        void B2SSetCanPlay(object idORvalue, object value);
        
        [DispId(125)]
        void B2SSetBallInPlay(object idORvalue, object value);
        
        [DispId(126)]
        void B2SSetTilt(object idORvalue, object value);
        
        [DispId(127)]
        void B2SSetMatch(object idORvalue, object value);
        
        [DispId(128)]
        void B2SSetGameOver(object idORvalue, object value);
        
        [DispId(129)]
        void B2SSetShootAgain(object idORvalue, object value);
        
        [DispId(130)]
        void B2SStartAnimation(string animationname, bool playreverse);
        
        [DispId(131)]
        void B2SStartAnimationReverse(string animationname);
        
        [DispId(132)]
        void B2SStopAnimation(string animationname);
        
        [DispId(133)]
        void B2SStopAllAnimations();
        
        [DispId(134)]
        bool B2SIsAnimationRunning(string animationname);
        
        [DispId(135)]
        void StartAnimation(string animationname, bool playreverse);
        
        [DispId(136)]
        void StopAnimation(string animationname);
        
        [DispId(137)]
        void B2SStartRotation();
        
        [DispId(138)]
        void B2SStopRotation();
        
        [DispId(139)]
        void B2SShowScoreDisplays();
        
        [DispId(140)]
        void B2SHideScoreDisplays();
        
        [DispId(141)]
        void B2SStartSound(string soundname);
        
        [DispId(142)]
        void B2SPlaySound(string soundname);
        
        [DispId(143)]
        void B2SStopSound(string soundname);
        
        [DispId(144)]
        void B2SMapSound(object digit, string soundname);
    }
}
