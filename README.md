# New Egg Stock Monitor

Wrote this to monitor NewEgg's stock of the AMD Ryzen9 5900X CPU

I have email, sms, and browser notifications setup via [nowinstock.net](https://www.nowinstock.net/) for Amazon and B&H's listing of the 5900X

I have [BirdBot](https://nateskicks13.gitbook.io/bird-bot/) monitoring BestBuy.com's listing

That left NewEgg, I had no way to monitor that as they don't offer an in-stock email notification themselves.

This solved that problem for me.

## Operating System Limitation Notes
The console app uses the Windows OS user32.dll to grab the window and bring it to the foreground if the 5900X becomes in stock

This means a few things. One, it'd probably be best to put the window in the middle of your primary monitor, then minimize it.
That way when the item comes in stock the window will pop up right in the middle of your screen and grab your attention

Two, this won't work on linux/mac. I'd have put in funtionality to check for OS before trying to use the .dll, but this is a quick and dirty utility app that's not meant to be overly feature complete.

## How To Use The SMS Notification Functionality

*Tl:dr:*
  * 1: You need to have a GMail account to get SMS Notifications
    * If that sounds confusing maybe you shouldn't be reading the tl;dr and should read this whole thing
  * 2: Navigate to https://myaccount.google.com/security
  * 3: Sign in with the account you intend to have the Console App use
  * 4: Scroll down to the card titled, "Less secure app access"
  * 5: Select, "Turn on access"
  * 6: Start the console app and answer the SMS-notification-related questions to enable SMS notifications
  * 7: (Optional) - Once you no longer use this app go back and turn off, "Less secure app access"
    * For real I don't claim to know the full consequences of enabling that, and take no responsibility for any risks associated with enabling it.
    * You choose to enable it at your own risk to use this console app's SMS notification functionality.
    
So because my console app can't just auto-buy the 5900X for me, it basically becomes a useless app once I'm not in front of my comp anymore.

So I figured I'd setup SMS notifications so it'll tell me when it determined NewEgg has the 5900X in stock so I can spring into action.

But I'm not tryna pay for Twilio or no shenanigans like that. So I set this up to send texts in a way that doesn't cost me $$$.

Most major phone carriers in the US host an email address where whatever email you send it, they'll turn around and send it as a text to a phone.

For example AT&T's address is [PHONE_NUMBER]@txt.att.net

But, I'm also not tryna host my own SMTP server or anything crazy here. Thankfully gmail publicly hosts one you can send web requests to for free.

So the console app has you provide your GMail account credentials and uses them to send an email from your account to your phone carrier who turns around and texts you the email it received.

Check out the tl;dr to see the prerequisite steps you need to take to get your google account setup so the Console App can use it to send emails for you

