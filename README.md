# Martingale-cBot

###### cAlgo Martingale strategy automated trading bot

** WARNING: This cBot was made for testing and education purposes only and does not guarantee any particular profit. **

This is my approach to make a pretty simple, yet fully automated cBot based on a well-known *'martingale strategy'*. In its classic version, it basically doubles the trade size after each loss, thus providing us with profit in the end (if your pockets are deep enough..).

![TDvaz8IAVu](https://user-images.githubusercontent.com/88622607/178152809-45900d24-4452-4aba-9fba-1f879e91ec56.gif)

This particular one was tested on a couple currency pairs at the 5-MIN timeframe. The signal was based on **McGinley Moving Average**, which is a popular indicator for determining trend direction (available online to download or code by yourself). For the mentioned timeframe, I found 20pips TP and 15pips SL to be working quite well on major currency pairs.

![stats](https://user-images.githubusercontent.com/88622607/178152820-bf779bf7-0e81-4218-928f-0564b7845c30.jpg)

The worst part of martingale strategy is the drawdown caused by multiplying the position size. In the ‘backtested’ period, the balance and equity drawdown were nearly 40% and 66% respectively. On the other hand, the cBots accuracy was just ~44% (which is clearly below 50-50), but it still managed to make profit well above 500%. That's undoubtedly a huge advantage of the Martingale system.

![curve](https://user-images.githubusercontent.com/88622607/178152822-807db0c1-956c-4e0b-abb5-fad9f3db1650.jpg)

Additional logic
- equity protection (example: set up when to close all trades when DD exceeds 25%)
- day of the week check (avoid opening new trades on early Monday + late Friday)
- avoid 'spikes' (hammer-like candles etc.)
- make breaks between trades (hard-coded bar counter)
