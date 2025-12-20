EXTERNAL StartFollowingRecentPlayer()
EXTERNAL StopFollowing()
== function StopFollowing
~ return
== function StartFollowingRecentPlayer
~ return

VAR IsFollowingPlayer = false

===ChickenRoot===
-> select(-> ChickenRandomStorylets) ->
*[Hi chicken]
    Bok (Hi)
    ~StartFollowingRecentPlayer()
    ~IsFollowingPlayer = true
*[bawk back]
    BAWKK (He looks offended..)
    ~StopFollowing()
    ~IsFollowingPlayer = false
- -> END

=== ChickenRandomStorylets ===
+ {req(IsFollowingPlayer == true)} ->
    Friendly Bok1
+ {req(IsFollowingPlayer == true)} ->
    Friendly Bok2
+ {req(IsFollowingPlayer == false)} ->
    Hostile Bok1
+ {req(IsFollowingPlayer == false)} -> 
    Hostile Bok2
- ->->
