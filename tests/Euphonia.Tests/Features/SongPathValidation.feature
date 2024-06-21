Feature: SongPathValidation

@EuphoniaTests
Scenario: When I download the song the name should be validated so there is no invalid characters
	Given The song file exists with a "<name>"
	When I verify the song name
	Then The "<expected name>" should not contain any invalid characters

Examples: 
| name                | expected name       |
| AmazingNAme##!!??<  | AmazingNAme##!!     |
| wowNAME#??:>        | wowNAME#            |
| wowNAME!\|*         | wowNAME!            |
| CoolSong\\".mp3     | CoolSong.mp3        |
| AppropriateSongName | AppropriateSongName |
