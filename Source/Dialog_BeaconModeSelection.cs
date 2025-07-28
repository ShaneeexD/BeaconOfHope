using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace BeaconOfHope
{
    public class Dialog_BeaconModeSelection : Window
    {
        private CompBeaconBroadcast beaconComp;
        private BeaconBroadcastMode selectedMode;
        
        // Increased window size
        private static readonly float WindowWidth = 400f;
        private static readonly float WindowHeight = 650f;
        
        public Dialog_BeaconModeSelection(CompBeaconBroadcast comp)
        {
            this.beaconComp = comp;
            this.selectedMode = comp.CurrentMode;
            
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = false; // Disable default close button to avoid overlap
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
        }
        
        public override Vector2 InitialSize => new Vector2(WindowWidth, WindowHeight);
        
        public override void DoWindowContents(Rect inRect)
        {
            // Title
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(0f, 0f, inRect.width, 42f);
            Widgets.Label(titleRect, "Beacon Broadcast Mode");
            Text.Font = GameFont.Small;
            
            // Main content area - reduced height to leave room for buttons
            Rect contentRect = new Rect(0f, 50f, inRect.width, inRect.height - 150f);
            
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(contentRect);
            
            // Mode selection with increased spacing
            DrawModeOption(listing, BeaconBroadcastMode.Off, "Off", 
                "The beacon is powered but not broadcasting any signal.");
                
            DrawModeOption(listing, BeaconBroadcastMode.WandererMode, "Wanderer Mode", 
                "Broadcasts a welcoming signal that increases the chance of wanderers joining your colony. Low power usage, minimal risk.");
                
            DrawModeOption(listing, BeaconBroadcastMode.EmergencyChannel, "Emergency Channel", 
                "Monitors emergency frequencies for distress signals, increasing the chance of escape pod crashes with survivors. Medium power usage, low risk.");
                
            DrawModeOption(listing, BeaconBroadcastMode.OpenBroadcast, "Open Broadcast", 
                "Broadcasts your colony's location widely, attracting refugees but also potentially hostile raiders who intercept the signal. High power usage, high risk.");
            
            listing.End();
            
            // Buttons at bottom with plenty of space
            float buttonWidth = 120f;
            float buttonHeight = 40f;
            
            // Position buttons with clear separation
            float cancelX = inRect.width / 4f - buttonWidth / 2f;
            float confirmX = (inRect.width * 3f) / 4f - buttonWidth / 2f;
            float buttonY = inRect.height - buttonHeight - 40f;
            
            // Cancel button
            if (Widgets.ButtonText(new Rect(cancelX, buttonY, buttonWidth, buttonHeight), "Cancel"))
            {
                Close();
            }
            
            // Confirm button
            if (Widgets.ButtonText(new Rect(confirmX, buttonY, buttonWidth, buttonHeight), "Confirm"))
            {
                beaconComp.SetMode(selectedMode);
                Close();
            }
        }
        
        private void DrawModeOption(Listing_Standard listing, BeaconBroadcastMode mode, string label, string description)
        {
            // Minimal spacing for each option
            listing.Gap(2f);
            
            Rect rowRect = listing.GetRect(35f);
            
            // Radio button
            Rect radioRect = new Rect(rowRect.x, rowRect.y, 24f, 24f);
            bool selected = selectedMode == mode;
            bool newSelected = Widgets.RadioButton(radioRect.x, radioRect.y, selected);
            
            if (newSelected && !selected)
            {
                selectedMode = mode;
            }
            
            // Label
            Rect labelRect = new Rect(radioRect.xMax + 10f, rowRect.y, rowRect.width - radioRect.width - 10f, rowRect.height);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(labelRect, label);
            
            // Description with increased height
            Rect descRect = listing.GetRect(70f);
            descRect.x += 34f;
            descRect.width -= 34f;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(descRect, description);
            
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            
            listing.Gap(10f);
        }
    }
}
