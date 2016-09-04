﻿using UnityEngine;
using System.Collections;
using AGS.Config;

namespace AGS.Characters
{
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(CapsuleCollider))]
	[RequireComponent(typeof(Animator))]
	public abstract class BaseCharacter : MonoBehaviour
	{
		[SerializeField] float m_MovingTurnSpeed = 360;
		[SerializeField] float m_StationaryTurnSpeed = 180;
		[SerializeField] float m_MoveSpeedMultiplier = 1f;
		[SerializeField] float m_GroundCheckDistance = 0.3f;
		[SerializeField] int   m_Health = 100;
		
		protected Rigidbody       m_Rigidbody;
		//Animator  m_Animator;
		protected float           m_TurnAmount;
		protected float           m_ForwardAmount;
		protected Vector3         m_GroundNormal;
		protected Vector3         m_MoveDirection;
		protected Vector3         m_VectorMask;
		protected Vector3         m_MoveTarget;
		public    GameObject      m_ActionTarget;
		protected GameObject      m_ActionPerformedTarget;
		protected Animator        m_Animator;
		protected bool            m_BlockMove;
		//float m_CapsuleHeight;
		protected Vector3         m_CapsuleCenter;
		protected CapsuleCollider m_Capsule;
		protected float           m_StunDuration;
		protected float           m_KnockBackSpeed;
		protected Vector3         m_KnockBackDirection;
		protected float           m_AbleToAttack;
		protected float           m_CanBeAttacked;
		protected float           m_SphereRadius;
		protected int             m_HitPoints;
		protected bool            m_IsDead;

		//Text m_text;
		
		protected void Start()
		{
			m_Animator = GetComponent<Animator>();
			m_Rigidbody = GetComponent<Rigidbody>();
			m_Capsule = GetComponent<CapsuleCollider>();
			//m_CapsuleHeight = m_Capsule.height;
			m_CapsuleCenter = m_Capsule.center;
			m_BlockMove = false;
			m_VectorMask = new Vector3 (1, 0, 1);
			//m_text = GameObject.Find ("Text").GetComponent<Text>();
			m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
			m_AbleToAttack = 1.0f;
			m_CanBeAttacked = 2.4f;
			m_SphereRadius = 0.8f;
			m_IsDead = false;
		}

		public void BlockMove(bool block)
		{
			m_BlockMove = block;
		}

		public void SetMoveTarget(Vector3 target)
		{
			m_MoveTarget = target;
			m_ActionTarget = null;
		}
		
		public void SetActionTarget(GameObject target)
		{
			m_ActionTarget = target;
			m_MoveTarget = Vector3.zero;
		}

		protected bool TargetInRange(float range, GameObject target)
		{
			Vector3 forward = transform.TransformDirection(Vector3.forward);
			Vector3 from = transform.TransformPoint (m_CapsuleCenter);
			RaycastHit hit;

			RaycastHit[] infos = Physics.SphereCastAll(from, 
			                                           m_SphereRadius, 
			                                           forward, 
			                                           range - m_SphereRadius);
			foreach(RaycastHit info in infos)
			{
				if(info.collider.gameObject == target)
				{
					return true;
				}
			}
			return false;
		}

		private void FollowTarget()
		{
			if (m_MoveTarget != Vector3.zero) {
				m_Animator.SetBool("HasTarget", false);
				if (Vector3.Distance (transform.position, m_MoveTarget) > 0.2f) {
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
				if(m_ActionTarget.gameObject.GetComponent<BaseCharacter>().IsDead())
				{
					m_ActionTarget = null;
					m_Animator.SetBool("HasTarget", false);
				}
				else
				{
					m_Animator.SetBool("HasTarget", true);
					if(!TargetInRange(m_AbleToAttack, m_ActionTarget))
					{
						m_MoveDirection = m_ActionTarget.transform.position - transform.position;
						m_MoveDirection = Vector3.Scale (m_MoveDirection, m_VectorMask).normalized;
						m_Animator.SetBool ("Run", true);
						m_Animator.SetBool("NormalAttack", false);
					}
					else
					{
						m_Animator.SetBool("NormalAttack", true);
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
		}
		
		private void Update()
		{
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

		void OnAnimatorMove()
		{
			if(m_Animator.GetBool("Run") == true && !IsDead() && !m_BlockMove)
			{
				Move (m_MoveDirection);
			}
	    }

		private void FixedUpdate()
		{
			if (m_KnockBackSpeed > 0 && m_KnockBackDirection != Vector3.zero) 
			{
				m_KnockBackDirection.Normalize();
				Vector3 moveAmount = (m_KnockBackSpeed * m_KnockBackDirection) * Time.deltaTime;
				m_KnockBackSpeed -= GameConstants.KnockBackSpeedDecreaseRate;
				m_Rigidbody.MovePosition (m_Rigidbody.position + moveAmount);
			}
			FollowTarget ();
		}
		
		private void Move(Vector3 move)
		{
			if (move == Vector3.zero)
				return;
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
		
		void ApplyMove(Vector3 move)
		{
			move = new Vector3 (move.x, 0, move.z);
			Vector3 moveAmount = Vector3.zero;
			moveAmount = move * Time.deltaTime * m_MoveSpeedMultiplier;
			m_Rigidbody.MovePosition (m_Rigidbody.position + moveAmount);
			
		}
		
		void ApplyExtraTurnRotation()
		{
			// help the character turn faster (this is in addition to root rotation in the animation)
			float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
			transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);
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
		
		protected void ActionCallBack (string name){}

		public void Attacked(AttackItem para)
		{
			//Debug.Log (para.m_HitPoint);
			m_HitPoints -= para.Damage;
			if (m_HitPoints <= 0) 
			{
				Debug.Log("DIe");
				m_Animator.SetTrigger ("Die");

				m_IsDead = true;
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

		public bool IsDead()
		{
			return m_IsDead;
		}

		private void StartStun(float stun)
		{
			Debug.Log ("Start Stun");
			float m_MaxStun = GameConstants.MaxStunTime;
			m_StunDuration = m_MaxStun * (stun/GameConstants.MaxStunPower);
			m_Animator.SetBool ("Stun", true);
			m_BlockMove = true;
		}

		private void StartKnockBack(float knockBack, Vector3 dir)
		{
			Debug.Log ("Start knockBack");
			float m_MaxKnockBackSpeed = GameConstants.MaxKnockBackSpeed;
			m_KnockBackSpeed = m_MaxKnockBackSpeed * (knockBack/GameConstants.MaxKnockBackPower);
			m_KnockBackDirection = dir;
		}

		public bool IsCurrentStateName(string name)
		{
			return m_Animator.GetCurrentAnimatorStateInfo (0).IsName (name);
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