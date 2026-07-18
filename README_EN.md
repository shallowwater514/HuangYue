# HuangYue: Seven Days of Grain

> *The ceremonial axe was never a symbol of emperors. It was the last thing taken up by those pushed to the brink.*
>
> A strategy game about leading 287 starving peasants through famine-torn Henan, 1641. You are not a nation. You are not a hero. You are someone trying to keep people alive for seven more days.

---

## This Is Not a Hero's Journey

Most strategy games let you be a nation-state, an interstellar empire, or a centuries-spanning dynasty. **HuangYue asks you to be a person with three days of grain and 287 mouths to feed.**

You are not Liu Bang founding a dynasty. You are not Zhu Yuanzhang rising from poverty to throne. You are someone making choices in a famine — and every choice costs someone something.

---

## What Makes This Different

### Morale is not a number. It's a mirror.

Traditional strategy games: Public Support +5 / -10, modify output efficiency.

**HuangYue**: There is no number telling you whether you are a righteous army or a bandit. The villagers tell you through their actions:

- **Righteous**: Village gates open. Children follow your column. Someone leaves a water jar by the road.
- **Bandit**: Bells ring before you enter. Every able-bodied person has fled to the hills. The well water tastes strange.
- **The gray zone**: Doors stay shut during the day. At night, someone sneaks grain out to your sentries.

**You may be the last to know what you have become.**

### Victory has no glory

Winning doesn't mean conquering a city. Winning means your supply carts cross the river, and several dozen people survive one more winter. The history books will not record your names — but Scholar Zhao's thin ledger records every one of your dead.

Defeat is not the fall of a nation. Defeat is the people beside your grain cart leaving no names behind, and an official ledger noting only "bandits dispersed."

---

## Current Version v0.4

### Seven-Day Narrative Mode
- Seven consecutive events, two choices each day
- Five state dimensions: Food, Followers, Morale, Public Support, Disease
- A hidden "Cruelty" score — you won't know it exists unless you look
- Four dynamic endings
- Five NPCs with branching epilogues: Widow Chen, Stone, Scholar Zhao, Han Jiu, Zhou Hesheng — each character's ending text is generated based on your choices throughout the game

### Real-Time Battle Mode (New)
- Ink-wash painting style tactical map
- Three commandable units: Main Force, Scout Cavalry, Supply Train
- Dijkstra pathfinding along road networks, smooth acceleration and deceleration
- **Pincer attacks**: Main Force and Scouts engage the same enemy from different approach vectors
- **Ambush stance**: Expanded interception radius + first-strike damage
- Objective: Defeat granary guards → Load supply train → Reach South Ferry
- Public support affects real-time combat: movement speed, combat power, whether locals guide your troops
- Procedurally-drawn event illustrations (six scene types: village, granary, refugees, wounded, rumor, blizzard)
- Dynamic incidents triggered by your state: roads blocked by villagers, volunteers joining, blizzards, rumors in camp

---

## v0.5 Design Direction (Discussion Welcome)

1. **De-numerify morale**: No more "Support: 52". Villager behavior reflects your moral position.
2. **Mirrored feedback**: Your actions → villagers' reactions → you see yourself through their eyes
3. **More non-combat dilemmas**: Starvation, plague, internal betrayal — war is not only lost on the battlefield
4. **No "correct" choices**: Every option has a cost. The difference is only who pays.

---

## How to Run

1. Download the ZIP from the latest Release
2. Extract to any folder
3. Double-click `黄钺-七日粮-Demo.exe`
4. If Windows shows "Unknown Publisher", click "More info → Run anyway"

> This is a personal prototype without a commercial code-signing certificate. Please verify that you downloaded it from this repository's Releases page.

**Requirements**: Windows 10 / 11. No runtime libraries needed.

---

## Tech Stack

- C# (.NET Framework)
- Pure Windows Forms — no third-party game engine
- All illustrations procedurally generated in code
- Ink-wash style map: real-time terrain, road, and unit rendering

---

## License

MIT License. You are free to use, modify, and distribute the code and assets.

**One request**: the stories, characters, and design philosophy are the author's heart. If you make something with this that you feel proud of, please let the author know.

---

## About the Author

A university student currently learning machine learning, with a dream of shipping a strategy game on Steam before senior year. This game was born from a single afternoon's thought: **Why does every strategy game teach me how to win, but none let me see what I have become?**

- If you played this and have thoughts, criticism, or want to contribute — open an Issue
- If you also want to make something "different" — come talk
- If you know how to make this better — send a PR

---

## Acknowledgments

To the friend who has played countless strategy games and said: *"Public support shouldn't be a stat. When support is gone, YOU are gone — not the peasants."*

That sentence became the direction for v0.5.
