﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AGS.Config;
using UnityEngine.UI;
using AGS.UI;
using AGS.World;

namespace AGS.Characters
{
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(CapsuleCollider))]
	[RequireComponent(typeof(Animator))]
	public abstract class BaseCharacter : AGS.World.OutLine
	{
		protected List<BaseAttributeListener> listeners = new List<BaseAttributeListener> ();
		protected float m_MovingTurnSpeed = 360;
		protected float m_StationaryTurnSpeed = 180;
		protected float m_GroundCheckDistance = 0.3f;

		//protected NavMeshAgent    m_Agent;
		protected Rigidbody       m_Rigidbody;
		//Animator  m_Animator;
		protected float           m_TurnAmount;
		protected float           m_ForwardAmount;
		protected Vector3         m_GroundNormal;
		protected Vector3         m_MoveDirection;
		protected Vector3         m_VectorMask;
		protected Vector3         m_MoveTarget;
		public    AGSAction       m_ActionTarget;
		protected AGSAction       m_ActionPerformedTarget;
		protected Animator        m_Animator;
		protected bool            m_BlockMove;
		//float m_CapsuleHeight;
		protected Vector3         m_CapsuleCenter;
		protected CapsuleCollider m_Capsule;
		protected float           m_StunDuration;
		protected float           m_KnockBackSpeed;
		protected Vector3         m_KnockBackDirection;
		public    float           m_AbleToAttack;
		public    float           m_AbleToAttackRadius;
		public    float           m_CanBeAttacked;
		public    float           m_CanBeAttackedRadius;
		protected bool            m_IsDead;
		protected float           m_LastSecond;
		protected float           m_TargetDirectionUpdateRate;
		protected float           m_LastDirectionUpdateTime;

		public    float           m_MaxHealth;
		public    float           m_Health;
		public    float           m_MaxStamina;
		public    float           m_Stamina;
		public    float           m_BasicPower;
		public    float           m_BasicAttackDistance;
		public    float           m_BasicAttackSpeed;
		public    float           m_MoveSpeed;
		public    int             m_Buff;
		public    float           m_TurnMultiplier; 

		//血条显示
		public    Canvas          m_HealthCanvas;
		private   Slider          m_HealthSlider;
		private   HealthBar       barClass;
		private   float           pn = 0f;
		private   NavMeshPath path;
		//Text m_text;
		public    Animator        Animator
		{
			get
			{
				return m_Animator;
			}
		}

		protected void Start()
		{
			base.Start();
			/*Debug.Log ("1:30  " + Mathf.Atan2(0.5f, 0.5f));
			Debug.Log ("3:00  " + Mathf.Atan2(1f, 0f));
			Debug.Log ("4:30  " + Mathf.Atan2(0.5f, -0.5f));
			Debug.Log ("6:00  " + Mathf.Atan2(0f, -1f));
			Debug.Log ("7:30  " + Mathf.Atan2(-0.5f, -0.5f));
			Debug.Log ("9:00  " + Mathf.Atan2(-1f, 0f));
			Debug.Log ("10:30 " + Mathf.Atan2(-0.5f, 0.5f));
			Debug.Log ("12:00 " + Mathf.Atan2(0f, 1f));*/

			m_Animator = GetComponent<Animator>();
			m_Rigidbody = GetComponent<Rigidbody>();
			m_Capsule = GetComponent<CapsuleCollider>();
			//m_CapsuleHeight = m_Capsule.height;
			m_CapsuleCenter = m_Capsule.center;
			m_BlockMove = false;
			m_VectorMask = new Vector3 (1, 0, 1);
			//m_text = GameObject.Find ("Text").GetComponent<Text>();
			m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
			//m_AbleToAttack = 1.0f;
			//m_CanBeAttacked = 2.4f;
			//m_SphereRadius = 0.8f;
			m_IsDead = false;
			m_LastSecond = Time.time;
			m_TargetDirectionUpdateRate = 0f;
			m_LastDirectionUpdateTime = 0f;
			m_TurnMultiplier = 1f;
			//m_Agent = GetComponentInChildren<NavMeshAgent>();
			//m_Agent.enabled = false;

			//path = new NavMeshPath();
		}

		public void BlockMove(bool block)
		{
			m_BlockMove = block;
		}

		public void SetMoveTarget(Vector3 target)
		{
			m_MoveTarget = target;
			if (m_ActionTarget != null) {
				m_ActionTarget.BeforeChangeTarget();
			}
			m_ActionTarget = null;
		}
		
		public void SetActionTarget(AGSAction target)
		{
			if (m_ActionTarget != null) {
				m_ActionTarget.BeforeChangeTarget();
			}
			m_ActionTarget = target;
			m_MoveTarget = Vector3.zero;
		}

		public bool TargetInRange(float range, float radius,GameObject target)
		{
			Vector3 forward = transform.TransformDirection(Vector3.forward);
			Vector3 from = transform.TransformPoint (m_CapsuleCenter + new Vector3(0f, -(m_Capsule.height/6f), 0f));
			//RaycastHit hit;

			RaycastHit[] infos = Physics.SphereCastAll(from, 
			                                           radius, 
			                                           forward, 
			                                           range - radius);
			foreach(RaycastHit info in infos)
			{
				if(info.collider.gameObject == target)
				{
					return true;
				}
			}
			return false;
		}

		protected virtual void FollowTarget()
		{
			if (m_MoveTarget != Vector3.zero) {
				m_Animator.SetBool("HasTarget", false);
				Vector3 mf = new Vector3 (transform.position.x, 0, transform.position.z);
				Vector3 tf = new Vector3 (m_MoveTarget.x, 0, m_MoveTarget.z);
				if (Vector3.Distance (mf, tf) > 0.2f) {
					m_MoveDirection = m_MoveTarget - transform.position;
					m_MoveDirection = Vector3.Scale (m_MoveDirection, m_VectorMask).normalized;
					m_Animator.SetBool ("Run", true);

				} else {
					m_MoveTarget = Vector3.zero;
					m_Animator.SetBool ("Run", false);
				}
			}
			else if(m_ActionTarget != null)
			{
				BaseCharacter baseCharacter = m_ActionTarget.TargetObj().GetComponent<BaseCharacter>();
				if(baseCharacter != null && baseCharacter.IsDead())
				{
					m_ActionTarget = null;
					m_Animator.SetBool("HasTarget", false);
				}
				else
				{
					m_Animator.SetBool("HasTarget", true);
					UpdateDirection();
					if(!m_ActionTarget.CanStartAction())
					{

						m_Animator.SetBool ("Run", true);
						m_ActionTarget.StopAction();
					}
					else
					{
						m_ActionTarget.StartAction();
						m_Animator.SetBool ("Run", false);
						m_ActionPerformedTarget = m_ActionTarget;
						
					}
				}
			}
			else 
			{
				m_Animator.SetBool("HasTarget", false);
				m_Animator.SetBool("Run", false);
			}

			if(m_Animator.GetBool("Run") == true && !IsDead() && !m_BlockMove)
			{
				Move (m_MoveDirection);
			}
		}

		/*protected virtual void FollowTarget()
		{
			if (m_MoveTarget != Vector3.zero) {
				m_Animator.SetBool("HasTarget", false);
				Vector3 mf = new Vector3 (transform.position.x, 0, transform.position.z);
				Vector3 tf = new Vector3 (m_MoveTarget.x, 0, m_MoveTarget.z);
				if (Vector3.Distance (mf, tf) > 0.2f) {
					// If agent is null, control movement by my script
					if (m_Agent.enabled == false) {
						m_MoveDirection = m_MoveTarget - transform.position;
						m_MoveDirection = Vector3.Scale (m_MoveDirection, m_VectorMask).normalized;
					} else {
						m_Agent.SetDestination (m_MoveTarget);
					}
					//NavMeshPath path = new NavMeshPath();
					//m_Agent.CalculatePath(m_MoveTarget, path);
					//foreach (Vector3 c in path.corners) {
						//Gizmos.DrawSphere (c, 0.5f);
					//	Debug.DrawRay(c, Vector3.up * 3, Color.green, 1f);
					//}
					//m_MoveDirection = path.corners [0] - transform.position;
					//m_MoveDirection = m_Agent.desiredVelocity;
					//NavMeshPath path = new NavMeshPath();
					//m_Agent.CalculatePath(transform.position, path);
					//m_MoveDirection = path.corners [0] - transform.position;

					m_Animator.SetBool ("Run", true);

				} else {
					m_MoveTarget = Vector3.zero;
					m_Animator.SetBool ("Run", false);
				}
			}
			else if(m_ActionTarget != null)
			{
				BaseCharacter baseCharacter = m_ActionTarget.TargetObj().GetComponent<BaseCharacter>();
				if(baseCharacter != null && baseCharacter.IsDead())
				{
					m_ActionTarget = null;
					m_Animator.SetBool("HasTarget", false);
				}
				else
				{
					m_Animator.SetBool("HasTarget", true);
					UpdateDirection();
					if(!m_ActionTarget.CanStartAction())
					{

						m_Animator.SetBool ("Run", true);
						m_ActionTarget.StopAction();
					}
					else
					{
						m_ActionTarget.StartAction();
						m_Animator.SetBool ("Run", false);
						m_ActionPerformedTarget = m_ActionTarget;

					}
				}
			}
			else 
			{
				m_Animator.SetBool("HasTarget", false);
				m_Animator.SetBool("Run", false);
			}

			if(m_Animator.GetBool("Run") == true && !IsDead() && !m_BlockMove && m_Agent.enabled == false)
			{
				Move (m_MoveDirection);
			}
		}*/

		public bool IsActionTargetTheSame()
		{
			if (m_ActionTarget != null && m_ActionPerformedTarget != null && m_ActionTarget == m_ActionPerformedTarget) {
				return true;
			} else {
				return false;
			}
		}

		public void ClearActionTarget()
		{
			m_ActionTarget = null;
		}

		private void UpdateDirection()
		{
			if(m_TargetDirectionUpdateRate <= 0f)
			{
				m_MoveDirection = m_ActionTarget.TargetObj().transform.position - transform.position;
				m_MoveDirection = Vector3.Scale (m_MoveDirection, m_VectorMask).normalized;
			}
			else if((Time.time - m_LastDirectionUpdateTime) >= m_TargetDirectionUpdateRate)
			{
				m_MoveDirection = m_ActionTarget.TargetObj().transform.position - transform.position;
				m_MoveDirection = Vector3.Scale (m_MoveDirection, m_VectorMask).normalized;
				m_LastDirectionUpdateTime = Time.time;
			}
		}

		/*private void UpdateDirection()
		{
			if(m_TargetDirectionUpdateRate <= 0f)
			{
				if (m_Agent.enabled == false) {
					m_MoveDirection = m_ActionTarget.TargetObj().transform.position - transform.position;
					m_MoveDirection = Vector3.Scale (m_MoveDirection, m_VectorMask).normalized;
				} else {
					m_Agent.SetDestination (m_ActionTarget.TargetObj().transform.position);
				}
					
				//m_MoveDirection = Vector3.Scale (m_MoveDirection, m_VectorMask).normalized;
			}
			else if((Time.time - m_LastDirectionUpdateTime) >= m_TargetDirectionUpdateRate)
			{
				if (m_Agent.enabled == false) {
					m_MoveDirection = m_ActionTarget.TargetObj().transform.position - transform.position;
					m_MoveDirection = Vector3.Scale (m_MoveDirection, m_VectorMask).normalized;
				} else {
					m_Agent.SetDestination (m_ActionTarget.TargetObj().transform.position);
				}

				m_LastDirectionUpdateTime = Time.time;
			}
		}*/

		public void AddAttributeListener(BaseAttributeListener listener)
		{
			listeners.Add (listener);
		}

		protected void ChangeMaxHealth(float value)
		{
			float ori = m_MaxHealth;
			m_MaxHealth += value;
			MaxHealthChanged(ori, m_MaxHealth);
		}

		protected void ChangeMaxStamina(float value)
		{
			float ori = m_MaxStamina;
			m_MaxStamina += value;
			MaxStaminaChanged(ori, m_MaxStamina);
		}

		protected void HealthChanged(float ori, float cur)
		{
			foreach (BaseAttributeListener l in listeners)
			{
				l.OnHealthChange(ori, cur);
			}
		}

		protected void BasicPowerChanged(float ori, float cur)
		{
			foreach (BaseAttributeListener l in listeners)
			{
				l.OnBasicPowerChange(ori, cur);
			}
		}

		protected void MaxHealthChanged(float ori, float cur)
		{
			foreach (BaseAttributeListener l in listeners)
			{
				l.OnMaxHealthChange(ori, cur);
			}
		}

		protected void MaxStaminaChanged(float ori, float cur)
		{
			foreach (BaseAttributeListener l in listeners)
			{
				l.OnMaxStaminaChange(ori, cur);
			}
		}

		protected void StaminaChanged(float ori, float cur)
		{
			foreach (BaseAttributeListener l in listeners)
			{
				l.OnStaminaChange(ori, cur);
			}
		}
		
		private void Update()
		{
			PerSecondUpdate ();
			DealWithStun ();
			//Vector3 forward = transform.TransformDirection(Vector3.forward);
			//Vector3 from = transform.TransformPoint (m_Rigidbody.centerOfMass);
			//Debug.DrawRay(from, forward, Color.red, 0.1f);
			//Debug.DrawLine (from, from + (forward.normalized * 2), Color.red, 0.1f);
		}

		private void DealWithStun()
		{
			if (m_StunDuration > 0f) 
			{
				m_StunDuration -= Time.deltaTime;
				if(m_StunDuration <= 0f)
				{
					m_BlockMove = false;
					m_Animator.SetBool("Stun", false);
				}
			}
	    }

		/*void OnAnimatorMove()
		{
			if(m_Animator.GetBool("Run") == true && !IsDead() && !m_BlockMove)
			{
				Move (m_MoveDirection);
			}
	    }*/

		private void FixedUpdate()
		{
			CalculateKnockBack ();
			FollowTarget ();
		}

		private void PerSecondUpdate()
		{
			if( (Time.time - m_LastSecond) >= 1f)
			{
				UpdateBaseAttributes();
				UpdateSubAttributes();
				m_LastSecond = Time.time;
			}
		}

		protected void UpdateBaseAttributes()
		{
			UpdateStamina ();
		}

		protected abstract void UpdateSubAttributes ();

		protected virtual void UpdateStamina(){}

		private void CalculateKnockBack()
		{
			if (m_KnockBackSpeed > 0 && m_KnockBackDirection != Vector3.zero) 
			{
				m_KnockBackDirection.Normalize();
				Vector3 moveAmount = (m_KnockBackSpeed * m_KnockBackDirection) * Time.deltaTime;
				m_KnockBackSpeed -= GameConstants.KnockBackSpeedDecreaseRate;
				m_Rigidbody.MovePosition (m_Rigidbody.position + moveAmount);
			}
		}
		
		protected void Move(Vector3 move)
		{
			if (move == Vector3.zero)
				return;
			move = Vector3.Scale (move, m_VectorMask).normalized;
			// convert the world relative moveInput vector into a local-relative
			// turn amount and forward amount required to head in the desired
			// direction.
			//Vector3 ori_move = move;
			Vector3 ori_move = move;
			if (move.magnitude > 1f) move.Normalize();
			move = transform.InverseTransformDirection(move);
			//m_text.text = "move: " + ori_move + " local move: " + move;
			CheckGroundStatus ();
			move = Vector3.ProjectOnPlane(move, m_GroundNormal);
			m_TurnAmount = Mathf.Atan2(move.x, move.z);
			m_ForwardAmount = move.z;
			//Debug.Log ("m_ForwardAmount: " + m_ForwardAmount);
			ApplyExtraTurnRotation();
			ApplyMove (ori_move);
			// control and velocity handling is different when grounded and airborne:
			//HandleGroundedMovement(move);
			
			
			//ScaleCapsuleForCrouching(crouch);
			//PreventStandingInLowHeadroom();
			
			// send input and other state parameters to the animator
			//UpdateAnimator(move);
		}
		
		/*
        public void RotateTo()
        {
            float current = transform.eulerAngles.y;
            this.transform.LookAt (m_currentNode.transform);
            Vector3 target = this.transform.eulerAngles;
            float next = Mathf.MoveTowardsAngle(current, target.y, 120 * Time.deltaTime);
            this.transform.eulerAngles = new Vector3(0, next, 0);
        }

        public void MoveTo()
        {
            Vector3 pos1 = transform.position;
            Vector3 pos2 = m_currentNode.transform.position;
            float dist = Vector2.Distance(new Vector2(pos1.x, pos1.z), new Vector2(pos2.x, pos2.z));
            if(dist < 1.0f)
            {
                if(m_currentNode.m_next == null){
                    Destroy(this.gameObject);
                }
                else
                {
                    m_currentNode = m_currentNode.m_next;
                }
            }
            transform.Translate (new Vector3(0, 0, speed * Time.deltaTime));
        }
		*/
		
		/*void UpdateAnimator(Vector3 move)
		{
			//m_Animator.SetFloat("Run", tru, 0.1f, Time.deltaTime);
			if (m_ForwardAmount > 0.0f) 
			{
				//Debug.Log("Move");
				m_Animator.SetBool("Run", true);
			}
			else
			{
				//Debug.Log("Stop");
				m_Animator.SetBool("Run", false);
			}
			
		}*/
		
		/*void HandleGroundedMovement(Vector3 move)
		{
			Vector3 v = (m_Animator.deltaPosition * m_MoveSpeedMultiplier) / Time.deltaTime;

			// we preserve the existing y part of the current velocity.
			v.y = m_Rigidbody.velocity.y;
			m_Rigidbody.velocity = v;
		}*/
		
		protected virtual void ApplyMove(Vector3 move)
		{
			move = new Vector3 (move.x, 0, move.z);
			Vector3 moveAmount = Vector3.zero;
			moveAmount = move * Time.deltaTime * m_MoveSpeed;
			//m_Rigidbody.MovePosition (m_Rigidbody.position + moveAmount);
			//m_Rigidbody.AddRelativeForce(move * m_MoveSpeed);
			transform.position = transform.position + moveAmount;
		}

		/*void HandleGroundedMovement(bool crouch, bool jump)
		{
			// check whether conditions are right to allow a jump:
			if (jump && !crouch && m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded"))
			{
				// jump!
				m_Rigidbody.velocity = new Vector3(m_Rigidbody.velocity.x, m_JumpPower, m_Rigidbody.velocity.z);
				m_IsGrounded = false;
				m_Animator.applyRootMotion = false;
				m_GroundCheckDistance = 0.1f;
			}
		}*/

		void ApplyExtraTurnRotation()
		{
			// help the character turn faster (this is in addition to root rotation in the animation)
			//float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
			//transform.Rotate(0, m_TurnMultiplier * m_TurnAmount * turnSpeed * Time.deltaTime, 0);
			if (m_TurnAmount > 0f)
				pn = 1f;
			else if (m_TurnAmount < 0f)
				pn = -1f;
			else
				pn = 0f;
			float amount_current_frame = m_MovingTurnSpeed * Time.deltaTime;
			float turn_amount = Mathf.Abs(m_TurnAmount) * 180f / Mathf.PI;
			float final_turn = amount_current_frame >= turn_amount?turn_amount:amount_current_frame;
			transform.Rotate(0, final_turn * pn, 0);
		}
		
		/*public void OnAnimatorMove()
		{
			// we implement this function to override the default root motion.
			// this allows us to modify the positional speed before it's applied.
			if (Time.deltaTime > 0)
			{
				Vector3 v = (m_Animator.deltaPosition * m_MoveSpeedMultiplier) / Time.deltaTime;
				
				// we preserve the existing y part of the current velocity.
				v.y = m_Rigidbody.velocity.y;
				m_Rigidbody.velocity = v;
			}
		}*/
		
		void CheckGroundStatus()
		{
			RaycastHit hitInfo;
			#if UNITY_EDITOR
			// helper to visualise the ground check ray in the scene view
			//Debug.DrawLine(transform.position + (Vector3.up * 0.1f), transform.position + (Vector3.up * 0.1f) + (Vector3.down * m_GroundCheckDistance));
			#endif
			// 0.1f is a small offset to start the ray from inside the character
			// it is also good to note that the transform position in the sample assets is at the base of the character
			if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, m_GroundCheckDistance))
			{
				m_GroundNormal = hitInfo.normal;
				//m_Animator.applyRootMotion = true;
			}
			else
			{
				m_GroundNormal = Vector3.up;
				//m_Animator.applyRootMotion = false;
			}
		}
		
		protected abstract void ActionCallBack (string name);

		protected void ChangeHealth(float value)
		{
			float ori = m_Health;
			m_Health += value;
			if (m_Health > m_MaxHealth) {
				m_Health = m_MaxHealth;
			}
			HealthChanged (ori, m_Health);
			ShowHealthBar ();
		}

		public void Attacked(AttackItem para)
		{
			ChangeHealth (-para.Damage);

			if (m_Health <= 0) 
			{
				//Debug.Log("DIe");
				m_Animator.SetTrigger ("Die");

				m_IsDead = true;
				m_Rigidbody.useGravity = false;
				m_Capsule.enabled = false;

				OnDie();
				//this.gameObject.SetActive(false);
				return;
			}
			if (para.Stun > 0) 
			{
				//StartCoroutine(StartStun(para.Stun));
				StartStun(para.Stun);
			}
			else if(para.KnockBack > 0 && para.KnockBackDirection != Vector3.zero)
			{
				StartKnockBack(para.KnockBack, para.KnockBackDirection);
			}
		}

		protected virtual void OnDie (){}

		public bool IsDead()
		{
			return m_IsDead;
		}

		private void StartStun(float stun)
		{
			//Debug.Log ("Start Stun");
			float m_MaxStun = GameConstants.MaxStunTime;
			m_StunDuration = m_MaxStun * (stun/GameConstants.MaxStunPower);
			m_Animator.SetBool ("Stun", true);
			m_BlockMove = true;
		}

		private void StartKnockBack(float knockBack, Vector3 dir)
		{
			//Debug.Log ("Start knockBack");
			float m_MaxKnockBackSpeed = GameConstants.MaxKnockBackSpeed;
			m_KnockBackSpeed = m_MaxKnockBackSpeed * (knockBack/GameConstants.MaxKnockBackPower);
			m_KnockBackDirection = dir;
		}

		public bool IsCurrentStateName(string name)
		{
			return m_Animator.GetCurrentAnimatorStateInfo (0).IsName (name);
		}

        //控制角色血条显示
		private void ShowHealthBar()
		{
			if(m_HealthCanvas != null)
			{
				if(m_HealthSlider == null)
				{
					m_HealthSlider = m_HealthCanvas.GetComponentInChildren<Slider> ();
					barClass = m_HealthCanvas.GetComponent<HealthBar> ();
					//TODO:临时使用 m_MaxHealth 有初始值的时候再移除
					//m_MaxHealth = 200;

				}
				if (!m_HealthSlider.IsActive ()) {
					m_HealthCanvas.enabled = true;
					m_HealthSlider.enabled = true;
					//Slider取值范围(0~1)
					m_HealthSlider.value = 1 - m_Health / (float)m_MaxHealth;
					barClass.timer = 0;
					//Debug.Log ("slider.value" + m_HealthSlider.value);
				} else {
					m_HealthSlider.value = 1 - m_Health / (float)m_MaxHealth;
					barClass.timer = 0;
					//Debug.Log ("slider.value" + m_HealthSlider.value);
				}
				if(m_Health <= 0)
				{
					m_HealthCanvas.enabled = false;
					m_HealthSlider.enabled = false;
				}
				//Debug.Log ("slider.value"+m_HealthSlider.value);
			}
		}

		public Vector3 Position()
		{
			return transform.position;
		}
		/*private IEnumerator StartStun(int stun)
		{
			float m_MaxStun = 5.0f;
			float m_StunTime = m_MaxStun * (stun/10);
			m_Animator.SetBool ("Stun", true);
			Debug.Log ("Stun Time: " + m_StunTime);
			yield return new WaitForSeconds(m_StunTime);
			Debug.Log ("Release from Stun");
			m_Animator.SetBool ("Stun", false);
		}*/

	}
}
