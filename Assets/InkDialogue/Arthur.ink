EXTERNAL SpawnPot()

VAR HasMetArthur = false

=== ArthurRoot ===
{
- HasMetArthur == false:
    Nice to meet you.
    I'm Arthur. I'm a little random.
    ~HasMetArthur = true
- SharedVarBreak5PotsQuestState == "REQUIREMENTS_NOT_MET" || SharedVarBreak5PotsQuestState == "CAN_START":
    -> Break5PotsQuest.BeforeQuest ->
}
-> ArthurHub

-> END

=== ArthurHub ===
    -> select(->ArthurStorylets) -> 
    -> ArthurChoices
-> END

== ArthurStorylets ==
+ {r()} ->
    I like cheese
+ {r()} ->
    I like potatoes
+ {r()} ->
    I like turnips
- ->->
== ArthurChoices ==
+ {SharedVarBreak5PotsQuestState == "CAN_START"} [I'm a strong drunk person]
    -> Break5PotsQuest.StartingQuest
+ {SharedVarBreak5PotsQuestState == "IN_PROGRESS"} [Give me a pot]
    ~SpawnPot()
+ {SharedVarBreak5PotsQuestState == "CAN_FINISH"} [Done breaking those 5 pots]
    -> Break5PotsQuest.EndingQuest
* ->
- -> END
    
== Break5PotsQuest ==

= BeforeQuest
I'm on the lookout for a drunk person to help me break some pots
    + [Why do they need to be drunk?]
        Well I can't imagine a sober person agreeing
    + [I'll keep an eye out]
        Thank you
    - ->->
=StartingQuest
Help me break 5 pots
I'll give you an umami treat to all who help me
~StartQuest("Break5PotsQuest")
-> DONE
=EndingQuest
Wow, thanks for the help.
Here's your treat
~FinishQuest("Break5PotsQuest")
-> END