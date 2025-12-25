===DrunkCatRoot===
{Drink3PotionsQuestState}
{
- Drink3PotionsQuestState == "CAN_START": 
    -> Drink3PotionsQuest.CanStart
- else: -> DrunkCatHub
}


===DrunkCatHub===
-> select(-> DrunkCatStorylets) ->
-> DrunkCatChoices

-> END

=== DrunkCatChoices ===
+ {Drink3PotionsQuestState == "CAN_FINISH"} [I took 3 sips]
    -> Drink3PotionsQuest.CanFinish
* ->
- -> END


=== DrunkCatStorylets ===
+ {r()} ->
    Sips
+ {r()} ->
    Sip sips
+ {req(Drink3PotionsQuestState == "IN_PROGRESS")} ->
    I bet you don't have what it takes
    -> Drink3PotionsQuest.InProgress
+ {req(Drink3PotionsQuestState == "IN_PROGRESS")} ->
    Nobody can drink as much as I do
    -> Drink3PotionsQuest.InProgress
+ {req(Drink3PotionsQuestState == "FINISHED")} ->
    Yay! It's so nice to finally have a friend
+ {req(Drink3PotionsQuestState == "FINISHED")} ->
    Sorry if I seemed a wreck when you met me
    It's crazy what loneliness can do to you
- ->->

=== Drink3PotionsQuest
=CanStart
I can't take it anymore...
I need a drinking partner
* [I'll be your drinking partner]
  Really? Take 3 sips and come back to me
  ~StartQuest("Drink3PotionsQuest")
* [You sad fool]
    Leave me alone
- -> END
=InProgress
Have you drank 3 sips?
    + [Yes...]
        No you haven't... I can't smell it on you..
    + [No]
        Of course not
- ->->
=CanFinish
Wow? Really?
    ...
    I'm honestly shocked. I was certain I had the worst liver on the planet by far
    It's nice to know someone who can finally relate to my struggle
    :)
    ~FinishQuest("Drink3PotionsQuest")
-> DrunkCatHub