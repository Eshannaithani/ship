# Store listing template

Fill from the descriptor and the brief. Every number or feature named in the copy goes under "Claims to trace" so it can be grepped against the constants before submission (SOP section 3). Plain ASCII only.

## Limits

| Field | App Store | Google Play |
|---|---|---|
| Title | 30 | 30 |
| Subtitle / short description | 30 | 80 |
| Description | 4000 | 4000 |
| Keywords | 100 chars, comma separated | none (indexed from description) |
| What's new | 4000 | 500 |
| Screenshots | 6.7" and 6.5" phones, 12.9" iPad if supported | phone, 7" and 10" tablet |

## Template

    # <Game> listing, <platform>, version <x.y.z> (build <n>)

    Title: <name, 30>
    Subtitle or short description: <one line, hook first>
    Keywords (iOS): <comma separated, no spaces after commas, no name repeats>

    Description:
    <one-line hook>
    <what you do in the first minute>
    <progression, in the player's words>
    <feature bullets, plain hyphens>
    <social proof or studio line, optional>

    What's new:
    <three bullets, player-facing, no internal jargon>

    Screenshots: <paths from the latest Playtest run, in order of the story they tell>

    Claims to trace:
    - "<claim as written>" -> constant or call site that backs it
