# sumacon

# SumaconScript

## Data types

Data type | Descripton
-|-
String | Represents a sequence of zero or more Unicode characters.
Number | Stores 64-bit floating-point values.

## Comment

```python
# This is a comment
n = 1 + 2  # This is also a comment
```
## Statements

### Assign statement

```python
n = 1 + 2
```

### Conditional branch statement (if, elif, else)

'`...`' represent for any statement.

```python
if 0 < n and n < 10
  ...
elif n == 20 or n == 30
  ...
else
  ...
end

# One liner
if n == 1 ... end
if n == 1 ... else ... end
if n == 1 ... elif n == 2 ... else ... end

# Can use alternative styles
if n == 1:        # ':' (Colon)
  ...
elif n == 2 then  # 'then'
  ...
end
```

### Loop statement (for, repeat)

```python
for i in 0 to 10 * 3  # Loop 31 times
  ...
  if ... break end
  if ... continue end
end

# When a loop counter is unnecessary
repeat 10 * 3  # Loop 30 times
  ...
end

# Can use alternative styles
for i in 10:  # ':' (Colon)
  ...
end
repeat 10 do  # 'do'
  ...
end
```

### Exit statement

```python
...
exit  # Stop the script here
...
```

## Operators

Operator | Precedence | Description | Example
-|-|-|-
** | 7 | Exponentiation | 2 ** 3 -> 8
* | 6 | Multiplication | 2 * 3 -> 12
/ | 6 | Division | 2 / 3 -> 1.5
// | 6 | Floor division | 2 / 3 -> 1
% | 6 | Remainder | 5 % 2 -> 1
+ | 5 | Addition | 2 + 3 -> 5
- | 5 | Substruction | 5 - 2 -> 3
== | 4 | Equal | 3 == 3 -> 1
!= | 4 | Not equal | 3 != 3 -> 0
< | 4 | Less than | -
> | 4 | Greater than | -
<= | 4 | Less than or equal to | -
>= | 4 | Greater than or equal to | -
not | 3 | Logical negation | not 0 -> 1
and | 2 | Logical AND | 0 and 1 -> 0
or | 1 | Logical OR | 0 or 1 -> 1


## Functions

### Basic functions

Syntax | Description
-|-
str( number ) | Convert a value to string.
num( string ) | Convert a value to number.
abs( number ) | Get a absolute value.
min( n1, n2[, ...] ) | Get a minimum value in arguments.
max( n1, n2[, ...] ) | Get a maximum value in arguments.
floor( number ) | Largest integer less than or equal to the specified number.
ceil( number ) | Smallest integer greater than or equal to the specified number.
truncate( number ) | Get a integral part of a specified number.
round( number ) | Rounds a specified number to the nearest even integer.
len( string ), strlen( string ) | Return a length of string.
chr( number ) | Code to character. <br>e.g. chr( 97 ) -> 'a'
ord( string ) | Character to code. <br>e.g. ord( 'a' ) -> 97
slice( string[, start[, stop]]) | Take a part of string. <br>e.g. silce( "takoyakiki", 3, -2 ) -> 'oyaki'
random( ) | Get a random real number as (0.0 <= N < 1.0).
randrange( min, max[, step] ) | Get a random real number as (min <= N < max)
uniform( min, max ) | Get a random real number as (min <= N <= max).
randint( min, max ) | Get a random integer number as (min <= N <= max).

### Control functions

Syntax | Description
-|-
print( text ) | Output the specified string to console.
wait( duration ) | Wait for a specified time.  <br>duration: wait time in milliseconds.
beep( ) | Plays the sound of a beep.
beep( frequency ) | Same as above.
beep( frequency, duration ) | Same as above.
tap( x, y, duration ) | Tap screen.
tap( tno, x, y, duration ) | Tap the screen with the specified touch point number.
swipe( x, y, angle, distance, duration ) | Swipe screen.
swipe( tno, x, y, angle, distance, duration ) | Swipe screen with the specified touch point number.
touch_on( tno, x, y ) | Make touch point on the screen.
touch_move( tno, x, y, duration ) | Move coordinate of touch point.
touch_off( tno ) | Remove specified touch point.
adb( command ) | Execute the adb command synchronously and return standard output/error as string.
save_capture( ) | Save the screen capture to file.
rotate( angle ) | Rotates the screen by the specified angle based on the current angle. (Relative rotation)
rotate_to( angle ) | Rotates the screen to the specified angle. (Absolute rotation)

### Function parameters

#### wait

Name | Data type | Description | Range
-|-|-|-
duration | Number | Wait time in milliseconds. | 10 - 60000

#### beep

Name | Data type | Description | Range
-|-|-|-
frequency | Number | Sound frequency in Hz. (Default value is `1000`) | 100 - 20000
duration | Number | Play time in milliseconds. (Default value is `100`) | 10 - 60000

#### tap

Name | Data type | Description | Range
-|-|-|-
tno | Number | Number to distinguish the touch points. | 0 - 9
x, y | Number | Normalized coordinate of screen. <br>`0` is left/top, `1.0` is right/bottom. | -

#### swipe

Name | Data type | Description | Range
-|-|-|-
tno | Number | Number to distinguish the touch points. | 0 - 9 |
x, y | Number | Normalized coordinate of screen. <br>`0` is left/top, `1` is right/bottom.  | -
angle | Number | Angle in degrees of Swipe direction. <br>`0` is rightward, `90` is upword. | -360 - 360
distance | Number | Swipe distance represented by ratio of short side of screen.
duration | Number | Swipe time in milliseconds. | 10 - 60000

#### touch_on

Name | Data type | Description | Range
-|-|-|-
tno | Number | Number to distinguish the touch points. | 0 - 9 |
x, y | Number | Normalized coordinate of screen. <br>`0` is left/top, `1` is right/bottom.  | -

#### touch_move

Name | Data type | Description | Range
-|-|-|-
tno | Number | Target touch point number. | 0 - 9 |
x, y | Number | Normalized coordinate of screen. <br>`0` is left/top, `1` is right/bottom.  | -
duration | Number | Moving time in milliseconds. | 10 - 60000

#### touch_off

Name | Data type | Description | Range
-|-|-|-
tno | Number | Target touch point number. | 0 - 9 |

#### adb

Name | Data type | Description | Range
-|-|-|-
command | String | Argument for adb command. <br>e.g. `shell cd /sdcard`  | -

#### rotate

Name | Data type | Description | Range
-|-|-|-
angle | Number | Angle in degrees of screen based on current orientation. | -360 - 360 (Increments of 90)

#### rotate_to

Name | Data type | Description | Range
-|-|-|-
angle | Number | Angle in degrees of screen orientation. | -360 - 360 (Increments of 90)
