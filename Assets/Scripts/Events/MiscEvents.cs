using System;
using UnityEngine;

public class MiscEvents
{
    public event EventHandler OnPotionDrank;
    public void InvokeOnPotionDrank(object sender) {
        OnPotionDrank?.Invoke(sender, EventArgs.Empty);
    }

    public class BingoBongoIncrementedEventArgs : EventArgs {
        public int newBingoBongoCount;
    }
    public event EventHandler<BingoBongoIncrementedEventArgs> OnBingoBongoIncremented;

    public void InvokeOnBingoBongoIncremented(object sender, int newCount) {
        OnBingoBongoIncremented?.Invoke(sender, new BingoBongoIncrementedEventArgs { newBingoBongoCount = newCount } );
    }

    public class BingoBongoChangedEventArgs : EventArgs {
        public int newBingoBongoCount;
    }
    public event EventHandler<BingoBongoChangedEventArgs> OnBingoBongoChanged;
    public void InvokeOnBingoBongoChanged(object sender, int newCount) {
        OnBingoBongoChanged?.Invoke(sender, new BingoBongoChangedEventArgs { newBingoBongoCount = newCount });
    }

    public event EventHandler<EventArgs> OnPotBroken;

    public void InvokeOnPotBroken(object sender)
    {
        OnPotBroken?.Invoke(sender, EventArgs.Empty);
    }
}