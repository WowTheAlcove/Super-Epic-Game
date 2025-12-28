INCLUDE RandomStoryletSelector.ink

//Dialogue Ink files to include
INCLUDE Chicken.ink
INCLUDE DrunkCat.ink
INCLUDE Arthur.ink

VAR Drink3PotionsQuestState = "REQUIREMENTS_NOT_MET"
VAR SharedVarBreak5PotsQuestState = "REQUIREMENTS_NOT_MET"
VAR testBool = true

EXTERNAL StartQuest(questId)
EXTERNAL AdvanceQuest(questId)
EXTERNAL FinishQuest(questId)
== function StartQuest(questId)
~return
== function AdvanceQuest(questId)
~return
== function FinishQuest(questId)
~return