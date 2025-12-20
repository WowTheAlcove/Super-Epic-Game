===DrunkCatRoot===
{
- Drink3PotionsQuestState == "CAN_START": 
    -> CanStartDrink3PotionsQuest
- else: -> DrunkCatHub
}


===DrunkCatHub===
-> select(-> DrunkCatStorylets) ->
-> DrunkCatChoices


=== DrunkCatChoices ===
+ {Drink3PotionsQuestState == "CAN_FINISH"} [I took 3 sips]
    Wow? Really?
    ...
    I'm honestly shocked. I was certain I had the worst liver on the planet by far
    It's nice to know someone who can finally relate to my struggle
    :)
    ~Drink3PotionsQuestState = "FINISHED"
* ->
- -> END


=== DrunkCatStorylets ===
+ {r()} ->
    Sips
+ {r()} ->
    Sip sips
+ {req(Drink3PotionsQuestState == "IN_PROGRESS")} ->
    I bet you don't have what it takes
    Have you drank 3 sips?
    // + [No]
        // Of course you haven't
+ {req(Drink3PotionsQuestState == "IN_PROGRESS")} ->
    Nobody can drink as much as I do
    Have you drank 3 sips?
    // + [No]
        // Of course not
+ {req(Drink3PotionsQuestState == "FINISHED")} ->
    Yay! It's so nice to finally have a friend
+ {req(Drink3PotionsQuestState == "FINISHED")} ->
    Sorry if I seemed a wreck when you met me
    It's crazy what loneliness can do to you
- ->->

=== CanStartDrink3PotionsQuest
I can't take it anymore...
I need a drinking partner
* [I'll be your drinking partner]
  Really? Take 3 sips and come back to me
  ~StartQuest("Drink3PotionsQuest")
* [You sad fool]
    Leave me alone
- -> END