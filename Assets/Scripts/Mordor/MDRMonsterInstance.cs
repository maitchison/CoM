using System;
using Data;
using UnityEngine;

namespace Mordor
{
	public enum MonsterTaskType
	{
		// Don't move.
		None,
		// Wander in a random direction.
		Wander,
		// Move towards party
		Charge,
		// Run away from party
		RunAway,
		// Try to attack party
		Attack
	}

	[DataObject("MonsterInstanceLibrary")]
	public class MDRMonsterInstanceLibrary : DataLibrary<MDRMonsterInstance>
	{
		public MDRMonsterInstanceLibrary()
		{
			AutoID = true;
		}
	}

	/** An instance of a monster, is able to move around the dungeon and attack. */
	[DataObject("MonsterInstance", true)]
	public class MDRMonsterInstance : NamedDataObject
	{
		/** The monster type we are. */
		[FieldAttr("MonsterType", FieldReferenceType.ID)]
		public MDRMonster MonsterType;

		/** The controller to send messages to when this monster performs actions. */
		[FieldAttr(true)]
		public MonsterController Controller;

		/** Used to adjust how fast this monster moves in relation to the monsters normal speed. */
		[FieldAttr(true)]
		public float MoveSpeedMultipler = 1f;

		/** Time before our next move command. */
		protected float TaskCooldown;

		// Location of this monster.
		public int X;
		public int Y;

		// Facing of monster.
		private Direction facing;

		// Which movement method to use.
		private MonsterTaskType currentTask = MonsterTaskType.Wander;

		/** The place we spawned from. */
		public MDRLocation SpawnLocation;

		/** The tile this monster is currently on. */
		public FieldRecord CurrentTile { get { return CoM.Party.Map[X, Y]; } }

		/** 
		 * Create a new monster instance of given type. 
		 * 
		 * @param monsterType The type of monster to spawn.		 
		 * 
		*/
		public static MDRMonsterInstance Create(MDRMonster monsterType)
		{			
			var result = new MDRMonsterInstance();				
			result.MonsterType = monsterType;
			return result;
		}

		/** Updates the actions of this monster, it might wander around or attack. */
		public void Update()
		{
			Util.Assert(MonsterType != null, "Update on monster instance with no monster type assigned.");

			updateAI();
			updateCurrentTask();
			updateController();
		}

		/** Returns if this monster can move foward or not */
		private bool canMoveForward()
		{			
			if (CurrentTile.GetTransit(facing) != TransitObstacal.None)
				return false;
			if (CoM.Party.Map.GetMonsterAtLocation(X + facing.DX, Y + facing.DY) != null)
				return false;
			return true;
		}

		/** Attempts to move the monster foward (assuming nothing is blocking them) */
		private void moveFoward()
		{
			if (canMoveForward()) {
				X += facing.DX;
				Y += facing.DY;
			}
		}

		private void turnLeft()
		{
			facing -= 90;
		}

		private void turnRight()
		{
			facing += 90;
		}

		/** Vector from monster to player. */
		private Vector2 deltaToPlayer {
			get { return new Vector2(X - CoM.Party.LocationX, Y - CoM.Party.LocationY); }
		}

		/** Returns the distance in tiles from spawn location. */
		private int getDistanceFromSpawnLocation()
		{
			// no spawn location.
			if (SpawnLocation.Floor == -1) {
				return 0;
			}

			//todo: we're assuming the correct floor number. 
			return (int)(new Vector2(SpawnLocation.X - X, SpawnLocation.Y - Y).magnitude);
		}

		/** Updates the decision making processes for this monster. */
		private void updateAI()
		{	
			int playerDistance = (int)deltaToPlayer.magnitude;
			bool hasSight = playerDistance < MonsterType.AgroRange;
			bool lostInterest = deltaToPlayer.magnitude > 1 + (MonsterType.AgroRange * 1.5f);

			if (hasSight) {
				if (playerDistance == 1)
					currentTask = MonsterTaskType.Attack;
				else
					currentTask = MonsterTaskType.Charge;
			}

			if (lostInterest)
				currentTask = MonsterTaskType.Wander;			
		}

		/** Processes the current task this monsters is trying to perform. */
		private void updateCurrentTask()
		{			
			// we move at half speed when wandering around.
			MoveSpeedMultipler = currentTask == MonsterTaskType.Wander ? 0.5f : 1f;

			TaskCooldown -= Time.deltaTime;

			if (TaskCooldown > 0)
				return;

			switch (currentTask) {
				case MonsterTaskType.None:
					TaskCooldown += 1f;
					break;
				case MonsterTaskType.Wander:
					// we just move around randomly.
					if (canMoveForward() && (Util.Roll(10) != 1)) {
						TaskCooldown += (MonsterType.MoveSpeed == 0) ? 0 : 1f / MonsterType.MoveSpeed;
						moveFoward();
					} else {
						if (Util.FlipCoin) {
							TaskCooldown += (MonsterType.MoveSpeed == 0) ? 0 : 0.1f / MonsterType.MoveSpeed;
							turnLeft();
						} else {
							TaskCooldown += (MonsterType.MoveSpeed == 0) ? 0 : 0.1f / MonsterType.MoveSpeed;
							turnRight();									
						}
					}

					break;

				case MonsterTaskType.Charge:
					// We move towards player as best we can
					facing.Angle = new Direction(deltaToPlayer.Angle()).Sector * 90;
					moveFoward();
					TaskCooldown += (MonsterType.MoveSpeed == 0) ? 0 : 1f / MonsterType.MoveSpeed;
					break;
									
			}

			if (TaskCooldown < 0)
				TaskCooldown = 0;

		}

		/** Updates the monster's avatar via a monster controller.  Monster instances may not have a controller
		 * in which case no update occurs. */
		private void updateController()
		{
			if (Controller == null)
				return;
		
			Controller.MoveTo(X, Y);
			Controller.TurnTo(facing);

			Controller.MOVE_SPEED = MonsterType.MoveSpeed * 2f;

		}

		public override string ToString()
		{
			return string.Format("{0} {1},{2}", MonsterType, X, Y);
		}

	}
}

