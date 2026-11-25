using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementScript : MonoBehaviour {
	Rigidbody rb;

	[Tooltip("Current players speed")]
	public float currentSpeed;
	[Tooltip("Assign players camera here")]
	[HideInInspector]public Transform cameraMain;
	[Tooltip("Force that moves player into jump")]
	public float jumpForce = 500;
    [Tooltip("ジャンプ中かのフラグ")]
    [HideInInspector] public bool isJumping;
    [Tooltip("Position of the camera inside the player")]
	[HideInInspector]public Vector3 cameraPosition;
	[Tooltip("プレイヤーの位置を示すシングルトン")]
	[HideInInspector] public static PlayerMovementScript instance;

    /*
	 * Getting the Players rigidbody component.
	 * And grabbing the mainCamera from Players child transform.
	 */
    void Awake(){
		rb = GetComponent<Rigidbody>();
		cameraMain = transform.Find("Main Camera").transform;
		bulletSpawn = cameraMain.Find ("BulletSpawn").transform;
		ignoreLayer = 1 << LayerMask.NameToLayer ("Player");

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

	}
	private Vector3 slowdownV;
	private Vector2 horizontalMovement;
	/*
	* Raycasting for meele attacks and input movement handling here.
	*/
	void FixedUpdate(){
		RaycastForMeleeAttacks ();

		// スライディング状態中ならスライディング動作の処理を行い、それ以外は通常の動作を行う
		if (isSliding) { SlidingMovementLogic(); }
		else { PlayerMovementLogic(); }
	}
	/**
	 * @brief スライディング動作の処理
	 * @details 
	 *	歩行・ダッシュ・しゃがみなど、プレイヤーが何らかの動作をしていない状態で
	 *	ダッシュ中にしゃがみを入力したときに起きる動作状態
	 * @param なし
	 * @return なし
	 */
	void SlidingMovementLogic()
	{
        // 毎フレーム少しずつ速度を減衰させる
        currentSlideSpeed = Mathf.MoveTowards(currentSlideSpeed, 0f, slideFriction * Time.fixedDeltaTime);

        // 一定の速度以下になったらスライディング解除
        if (currentSlideSpeed <= slideEndSpeed)
        {
            isSliding = false;
            return;
        }
        
		// 進行方向へ現在のスライディング速度を適用
		rb.linearVelocity = new Vector3(
			slideDirection.x * currentSlideSpeed,
			rb.linearVelocity.y,
			slideDirection.z * currentSlideSpeed);
		currentSpeed = rb.linearVelocity.magnitude;
	}
	/*
	* Accordingly to input adds force and if magnitude is bigger it will clamp it.
	* If player leaves keys it will deaccelerate
	*/
	void PlayerMovementLogic(){
		currentSpeed = rb.linearVelocity.magnitude;
		horizontalMovement = new Vector2 (rb.linearVelocity.x, rb.linearVelocity.z);
		if (horizontalMovement.magnitude > maxSpeed){
			horizontalMovement = horizontalMovement.normalized;
			horizontalMovement *= maxSpeed;    
		}
		rb.linearVelocity = new Vector3 (
			horizontalMovement.x,
			rb.linearVelocity.y,
			horizontalMovement.y
		);
		if (grounded){
			rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity,
				new Vector3(0,rb.linearVelocity.y,0),
				ref slowdownV,
				deaccelerationSpeed);
		}

		if (grounded) {
			rb.AddRelativeForce (Input.GetAxis ("Horizontal") * accelerationSpeed * Time.deltaTime, 0, Input.GetAxis ("Vertical") * accelerationSpeed * Time.deltaTime);
		} else {
			rb.AddRelativeForce (Input.GetAxis ("Horizontal") * accelerationSpeed / 2 * Time.deltaTime, 0, Input.GetAxis ("Vertical") * accelerationSpeed / 2 * Time.deltaTime);

		}
		/*
		 * Slippery issues fixed here
		 */
		if (Input.GetAxis ("Horizontal") != 0 || Input.GetAxis ("Vertical") != 0) {
			deaccelerationSpeed = 0.5f;
		} else {
			deaccelerationSpeed = 0.1f;
		}
	}
	/*
	* Handles jumping and ads the force and sounds.
	*/
	void Jumping(){
		if ((Input.GetKeyDown (KeyCode.Space) || Input.GetKeyDown(KeyCode.Joystick1Button0)) && grounded)
		{
			rb.AddRelativeForce (Vector3.up * jumpForce);
			if (_jumpSound)
				_jumpSound.Play ();
			else
				print ("Missig jump sound.");
			_walkSound.Stop ();
			_runSound.Stop ();
		}
	}
	/*
	* Update loop calling other stuff
	*/
	void Update(){
		

		Jumping ();

		Crouching();

		WalkingSound ();


	}//end update

	/*
	* Checks if player is grounded and plays the sound accorindlgy to his speed
	*/
	void WalkingSound(){
		if (_walkSound && _runSound) {
			if (RayCastGrounded ()) { //for walk sounsd using this because suraface is not straigh			
				if (currentSpeed > 1) {
					//				print ("unutra sam");
					if (maxSpeed == 3) {
						//	print ("tu sem");
						if (!_walkSound.isPlaying) {
							//	print ("playam hod");
							_walkSound.Play ();
							_runSound.Stop ();
						}					
					} else if (maxSpeed == 5) {
						//	print ("NE tu sem");

						if (!_runSound.isPlaying) {
							_walkSound.Stop ();
							_runSound.Play ();
						}
					}
				} else {
					_walkSound.Stop ();
					_runSound.Stop ();
				}
			} else {
				_walkSound.Stop ();
				_runSound.Stop ();
			}
		} else {
			print ("Missing walk and running sounds.");
		}

	}
	/*
	* Raycasts down to check if we are grounded along the gorunded method() because if the
	* floor is curvy it will go ON/OFF constatly this assures us if we are really grounded
	*/
	private bool RayCastGrounded(){
		RaycastHit groundedInfo;
		if(Physics.Raycast(transform.position, transform.up *-1f, out groundedInfo, 1, ~ignoreLayer)){
			Debug.DrawRay (transform.position, transform.up * -1f, Color.red, 0.0f);
			if(groundedInfo.transform != null){
				//print ("vracam true");
				return true;
			}
			else{
				//print ("vracam false");
				return false;
			}
		}
		//print ("nisam if dosao");

		return false;
	}

	/*
	* If player toggle the crouch it will scale the player to appear that is crouching
	*/
	void Crouching()
	{
		// しゃがみ入力（押し続けている）
		bool crouchInput = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.Joystick1Button3);
        // しゃがみ入力（押した瞬間）
        bool crouchDown  = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Joystick1Button3);

		// スライディング中の処理
		if(isSliding)
		{
			// しゃがみキーを離す、ジャンプをするなどの行動入力を受け取った場合、スライディング状態を解除する
			if (!crouchInput || Input.GetKeyDown(KeyCode.Space)) { isSliding = false; }
			// スライディング処理
			transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1,0.6f,1), Time.deltaTime * 15);
			return;
		}
		// ダッシュ中にしゃがみを押したとき、スライディング状態に移行する
		if (isRunning)
		{
			if (isRunning && crouchDown && grounded && currentSpeed >= minSpeedToSlide)
			{
				isSliding = true;
				isCrouching = false;
				currentSlideSpeed = currentSpeed * slideSpeedMultiplier;
				slideDirection = transform.forward;
				return;
			}
			// 一定の速度を越えていなければしゃがみ状態にする
			else if (currentSpeed < minSpeedToSlide) { isCrouching = crouchInput; }
			// 入力を解除した場合
			else { isCrouching = false; }
		}
		// 通常のしゃがみ状態
		else { isCrouching = crouchInput; }

		// 見た目の高さを変更
		if (isCrouching) { transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1, 0.6f, 1), Time.deltaTime * 15); }
		else			 { transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1,    1, 1), Time.deltaTime * 15); }
	}

    [Header("Sliding Settings")]
	[Tooltip("スライディング時の速度倍率")]
	[SerializeField] private float slideSpeedMultiplier = 1.6f; // 現在速度 × この倍率で滑る
    [Tooltip("ダッシュ中にこの速度以上でしゃがむとスライディング状態に移行する")]
    public float minSpeedToSlide = 4.0f;
    [Tooltip("この速度以下まで減速したらスライディング解除")]
    public float slideEndSpeed = 2.5f;
	[Tooltip("スライディング時の摩擦力")]
    public float slideFriction = 6.0f;	// 滑っている間の減速率（大きいほど早く止まる）
    [Tooltip("走っている状態")]
	[HideInInspector] public bool isRunning;  // GunScriptからダッシュ状態を受け取る
    [Tooltip("スライディング状態")]
    public bool isSliding { get; private set; }
    [Tooltip("しゃがんでいる状態")]
    public bool isCrouching { get; private set; }

    private float currentSlideSpeed = 0f;
    private Vector3 slideDirection;

    [Tooltip("The maximum speed you want to achieve")]
	public int maxSpeed = 5;
	[Tooltip("The higher the number the faster it will stop")]
	public float deaccelerationSpeed = 15.0f;


	[Tooltip("Force that is applied when moving forward or backward")]
	public float accelerationSpeed = 50000.0f;


	[Tooltip("Tells us weather the player is grounded or not.")]
	public bool grounded;
	/*
	* checks if our player is contacting the ground in the angle less than 60 degrees
	*	if it is, set groudede to true
	*/
	void OnCollisionStay(Collision other){
		foreach(ContactPoint contact in other.contacts){
			if(Vector2.Angle(contact.normal,Vector3.up) < 60){
				grounded = true;
			}
		}
	}
	/*
	* On collision exit set grounded to false
	*/
	void OnCollisionExit ()
	{
		grounded = false;
	}


	RaycastHit hitInfo;
	private float meleeAttack_cooldown;
	private string currentWeapo;
	[Tooltip("Put 'Player' layer here")]
	[Header("Shooting Properties")]
	private LayerMask ignoreLayer;//to ignore player layer
	Ray ray1, ray2, ray3, ray4, ray5, ray6, ray7, ray8, ray9;
	private float rayDetectorMeeleSpace = 0.15f;
	private float offsetStart = 0.05f;
	[Tooltip("Put BulletSpawn gameobject here, palce from where bullets are created.")]
	[HideInInspector]
	public Transform bulletSpawn; //from here we shoot a ray to check where we hit him;
	/*
	* This method casts 9 rays in different directions. ( SEE scene tab and you will see 9 rays differently coloured).
	* Used to widley detect enemy infront and increase meele hit detectivity.
	* Checks for cooldown after last preformed meele attack.
	*/


	public bool been_to_meele_anim = false;
	private void RaycastForMeleeAttacks(){




		if (meleeAttack_cooldown > -5) {
			meleeAttack_cooldown -= 1 * Time.deltaTime;
		}


		if (GetComponent<GunInventory> ().currentGun) {
			if (GetComponent<GunInventory> ().currentGun.GetComponent<GunScript> ()) 
				currentWeapo = "gun";
		}

		//middle row
		ray1 = new Ray (bulletSpawn.position + (bulletSpawn.right*offsetStart), bulletSpawn.forward + (bulletSpawn.right * rayDetectorMeeleSpace));
		ray2 = new Ray (bulletSpawn.position - (bulletSpawn.right*offsetStart), bulletSpawn.forward - (bulletSpawn.right * rayDetectorMeeleSpace));
		ray3 = new Ray (bulletSpawn.position, bulletSpawn.forward);
		//upper row
		ray4 = new Ray (bulletSpawn.position + (bulletSpawn.right*offsetStart) + (bulletSpawn.up*offsetStart), bulletSpawn.forward + (bulletSpawn.right * rayDetectorMeeleSpace) + (bulletSpawn.up * rayDetectorMeeleSpace));
		ray5 = new Ray (bulletSpawn.position - (bulletSpawn.right*offsetStart) + (bulletSpawn.up*offsetStart), bulletSpawn.forward - (bulletSpawn.right * rayDetectorMeeleSpace) + (bulletSpawn.up * rayDetectorMeeleSpace));
		ray6 = new Ray (bulletSpawn.position + (bulletSpawn.up*offsetStart), bulletSpawn.forward + (bulletSpawn.up * rayDetectorMeeleSpace));
		//bottom row
		ray7 = new Ray (bulletSpawn.position + (bulletSpawn.right*offsetStart) - (bulletSpawn.up*offsetStart), bulletSpawn.forward + (bulletSpawn.right * rayDetectorMeeleSpace) - (bulletSpawn.up * rayDetectorMeeleSpace));
		ray8 = new Ray (bulletSpawn.position - (bulletSpawn.right*offsetStart) - (bulletSpawn.up*offsetStart), bulletSpawn.forward - (bulletSpawn.right * rayDetectorMeeleSpace) - (bulletSpawn.up * rayDetectorMeeleSpace));
		ray9 = new Ray (bulletSpawn.position -(bulletSpawn.up*offsetStart), bulletSpawn.forward - (bulletSpawn.up * rayDetectorMeeleSpace));

		Debug.DrawRay (ray1.origin, ray1.direction, Color.cyan);
		Debug.DrawRay (ray2.origin, ray2.direction, Color.cyan);
		Debug.DrawRay (ray3.origin, ray3.direction, Color.cyan);
		Debug.DrawRay (ray4.origin, ray4.direction, Color.red);
		Debug.DrawRay (ray5.origin, ray5.direction, Color.red);
		Debug.DrawRay (ray6.origin, ray6.direction, Color.red);
		Debug.DrawRay (ray7.origin, ray7.direction, Color.yellow);
		Debug.DrawRay (ray8.origin, ray8.direction, Color.yellow);
		Debug.DrawRay (ray9.origin, ray9.direction, Color.yellow);

		if (GetComponent<GunInventory> ().currentGun) {
			if (GetComponent<GunInventory> ().currentGun.GetComponent<GunScript> ().meeleAttack == false) {
				been_to_meele_anim = false;
			}
			if (GetComponent<GunInventory> ().currentGun.GetComponent<GunScript> ().meeleAttack == true && been_to_meele_anim == false) {
				been_to_meele_anim = true;
				//	if (isRunning == false) {
				StartCoroutine ("MeeleAttackWeaponHit");
				//	}
			}
		}

	}

	/*
	 *Method that is called if the waepon hit animation has been triggered the first time via Q input
	 *and if is, it will search for target and make damage
	 */
	IEnumerator MeeleAttackWeaponHit(){
		if (Physics.Raycast (ray1, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast (ray2, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast (ray3, out hitInfo, 2f, ~ignoreLayer)
			|| Physics.Raycast (ray4, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast (ray5, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast (ray6, out hitInfo, 2f, ~ignoreLayer)
			|| Physics.Raycast (ray7, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast (ray8, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast (ray9, out hitInfo, 2f, ~ignoreLayer)) {
			//Debug.DrawRay (bulletSpawn.position, bulletSpawn.forward + (bulletSpawn.right*0.2f), Color.green, 0.0f);
			if (hitInfo.transform.tag=="Dummie") {
				Transform _other = hitInfo.transform.root.transform;
				if (_other.transform.tag == "Dummie") {
					print ("hit a dummie");
				}
				InstantiateBlood(hitInfo,false);
			}
		}
		yield return new WaitForEndOfFrame ();
	}

	[Header("BloodForMelleAttaacks")]
	RaycastHit hit;//stores info of hit;
	[Tooltip("Put your particle blood effect here.")]
	public GameObject bloodEffect;//blod effect prefab;
	/*
	* Upon hitting enemy it calls this method, gives it raycast hit info 
	* and at that position it creates our blood prefab.
	*/
	void InstantiateBlood (RaycastHit _hitPos,bool swordHitWithGunOrNot) {		

		if (currentWeapo == "gun") {
			GunScript.HitMarkerSound ();

			if (_hitSound)
				_hitSound.Play ();
			else
				print ("Missing hit sound");
			
			if (!swordHitWithGunOrNot) {
				if (bloodEffect)
					Instantiate (bloodEffect, _hitPos.point, Quaternion.identity);
				else
					print ("Missing blood effect prefab in the inspector.");
			}
		} 
	}
	private GameObject myBloodEffect;


	[Header("Player SOUNDS")]
	[Tooltip("Jump sound when player jumps.")]
	public AudioSource _jumpSound;
	[Tooltip("Sound while player makes when successfully reloads weapon.")]
	public AudioSource _freakingZombiesSound;
	[Tooltip("Sound Bullet makes when hits target.")]
	public AudioSource _hitSound;
	[Tooltip("Walk sound player makes.")]
	public AudioSource _walkSound;
	[Tooltip("Run Sound player makes.")]
	public AudioSource _runSound;
}

