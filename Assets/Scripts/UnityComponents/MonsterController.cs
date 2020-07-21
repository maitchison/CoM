using UnityEngine;
using System.Collections;
using Mordor;

/** 
 * Controls a monster avatar.  A monster instance will pass commands to this controler, which will in turn make sure
 * the avatar is in the correct state to reflect the monsters actions. 
 */
[ExecuteInEditMode]
public class MonsterController : MonoBehaviour
{
	private MDRMonsterInstance instance;

	/** Current position */
	[SerializeField]
	[HideInInspector]
	private int _x;
	[SerializeField]
	[HideInInspector]
	private int _y;
	[SerializeField]
	[HideInInspector]
	private Direction _facing;

	[SerializeField]
	[HideInInspector]
	private Sprite _sprite;

	public int X { get { return _x; } set { MoveTo(value, _y); } }

	public int Y { get { return _y; } set { MoveTo(_x, value); } }

	public Direction Facing { get { return _facing; } set { TurnTo(value); } }

	public Sprite Sprite { get { return _sprite; } set { setSprite(value); } }

	/** How fast the turning animation goes in degrees / second. */
	public float TURN_SPEED = 360f;
	/** How fast the movement speed goes in tiles per second. */
	public float MOVE_SPEED = 3f;

	/** Moves the monster to specified location. */
	public void MoveTo(int x, int y)
	{
		_x = x;
		_y = y;
	}

	/** Turns the monster to specified direction. */
	public void TurnTo(Direction facing)
	{
		_facing = facing;
	}

	/** Begins an attack animation. */
	public void Attack()
	{
	}

	/** Begins a die animation. */
	public void Die()
	{
	}

	/** Begins a was hit animation. */
	public void WasHit()
	{
	}

	void Update()
	{		
		if (Application.isPlaying)
			sync(Time.deltaTime * MOVE_SPEED, Time.deltaTime * TURN_SPEED);
		else
			sync();		
	}

	/** Forces avatar to instantly reflect the controllers position. */
	public void Resync()
	{
		sync();
	}

	/** 
	 * Syncs the avatar location and direction to our current location and position. 
	 * @param maxMove The maximum distance in tiles we can move this sync, null for unlimited.
	 * @param maxTurn The maximum amount we can turn in degrees this sync, null for unlimited.
	*/
	private void sync(float? maxMove = null, float? maxTurn = null)
	{		
		var moveDelta = new Vector3(X, 0, Y) - transform.position;

		if (maxMove != null) {
			moveDelta = moveDelta.Clamp((float)maxMove);
		}

		transform.position = transform.position + moveDelta;

		var turnDelta = Facing.Angle - transform.localRotation.eulerAngles.y;

		if (maxTurn != null) {
			turnDelta = Util.Clamp(turnDelta, -(float)maxTurn, +(float)maxTurn);
		}

		transform.localRotation = Quaternion.Euler(90, transform.localRotation.eulerAngles.y + turnDelta, 0);
	}

	/** 
	 * Sets the sprite for this avatar. 
	 */
	private void setSprite(Sprite sprite)
	{
		if (sprite == _sprite)
			return;
		
		var pixelMesh = gameObject.GetComponentInChildren<PixelMesh>();
		if (pixelMesh == null) {
			Trace.LogWarning("No pixelmesh in child objects of avatar {0}", this);
			return;
		}
		_sprite = sprite;
		pixelMesh.Sprite = sprite;
		pixelMesh.Apply();
	}

}
