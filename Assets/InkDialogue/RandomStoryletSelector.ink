//example usages ===========================
=== RandomTest1 ===
Time for a{|nother} storylet!
+ [Ok]
 -> select(-> example_storylets) -> RandomTest1

=== RandomTest2 ===
-> select(-> example_storylets) ->
-> END

=== example_storylets
<- complicated_quest
+ {r()} ->
    Thing 1 happened
+ {r()} ->
    Thing 2 happened.
+ {req(false)} ->
    Thing 3 happened (but it shouldn't have?).
+ (onetime) {req(not onetime)} ->
    Thing 4 happened (but can only happen once).
- ->->

= dialogueLine
    line of dialogue
    ->->

=== complicated_quest ===
/* You can of course group large storylets
or groups of chained/related/mutually-exclusive storylets into their own knot.*/
+ (quest1) {req(not quest1)} ->
    The quest has started.
+ (quest2) {req(quest1 and not quest2)} ->
    The quest continues.
+ (quest3) {req(quest2 and not quest3)} ->
    The end of the quest.
- ->->


// Storylet Code ============================
VAR _COUNT_MODE = true
VAR _STORYLET_COUNT = 1

=== select(-> storylets) ===
~_STORYLET_COUNT = 0
~_COUNT_MODE = true
<- storylets
+ ->
-
~_COUNT_MODE = false
~_STORYLET_COUNT += 1
<- storylets
+ ->-> // Optional: fallback for if no storylets are valid

// Could be expanded with weights/salience
=== function req(condition) ===
{_COUNT_MODE:
    {condition:
        ~_STORYLET_COUNT += 1
    }
    ~return false
- else:
    {condition:
        ~_STORYLET_COUNT -= 1
        ~return RANDOM(1,_STORYLET_COUNT) == 1 // Uniformly random! Do the math.
    -else:
        ~return false
    }
}

// Shorthand for storylets with no conditions
=== function r() ===
~return req(true)