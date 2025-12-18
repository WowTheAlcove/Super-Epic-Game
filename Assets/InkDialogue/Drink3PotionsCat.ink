===Drink3PotionsCatDefault===
*Sip*... *Sip*...
-> END

===Drink3PotionsQuest===
{ Drink3PotionsQuest :
    - "REQUIREMENTS_NOT_MET": -> requirementsNotMet
    - "CAN_START": -> canStart
    - "IN_PROGRESS": -> canFinish
    - "FINISHED": -> finished
    - else: -> END
}

= requirementsNotMet
Man, I love taking sips
You wanna take some sips?
* [Sure]
  Great! Take some sips and come back to me
* [No]
    Cmonn... all the cool kids are doing it
    Oh well
- -> END

= canStart
-> END

= canFinish
-> END
= finished
-> END