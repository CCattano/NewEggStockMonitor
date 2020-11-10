# New Egg Stock Monitor

Wrote this to monitor NewEgg's stock of the AMD Ryzen9 5900X CPU

I have email, sms, and browser notifications setup via [nowinstock.net](https://www.nowinstock.net/) for Amazon and B&H's listing of the 5900X

I have [BirdBot](https://nateskicks13.gitbook.io/bird-bot/) monitoring BestBuy.com's listing

That left NewEgg, I had no way to monitor that as they don't offer an in-stock email notification themselves.

This solved that problem for me.

## Important Usage Note
The console app uses the Windows OS user32.dll to grab the window and bring it to the foreground if the 5900X becomes in stock

This means a few things. One, it'd probably be best to put the window in the middle of your primary monitor, then minimize it.
That way when the item comes in stock the window will pop up right in the middle of your screen and grab your attention

Two, this won't work on linux/mac. I'd have put in funtionality to check for OS before trying to use the .dll, but this is a quick and dirty utility app that's not meant to be overly feature complete.
