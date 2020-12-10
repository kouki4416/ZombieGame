using UnityEngine;
using System.Collections;

public class CharacterManager : MonoBehaviour 
{
	// Inspector Assigned
	[SerializeField] private CapsuleCollider 	_meleeTrigger 		= null;
	[SerializeField] private CameraBloodEffect	_cameraBloodEffect 	= null;
	[SerializeField] private Camera				_camera				=null;
	[SerializeField] private float				_health				= 100.0f;
	[SerializeField] private AISoundEmitter		_soundEmitter		= null;
	[SerializeField] private float				_walkRadius			= 0.0f;
	[SerializeField] private float				_runRadius			= 7.0f;
	[SerializeField] private float				_landingRadius		= 12.0f;
	[SerializeField] private float				_bloodRadiusScale	= 6.0f;
	[SerializeField] private PlayerHUD _playerHUD = null;

	// Pain Damage Audio
	[SerializeField] private AudioCollection	_damageSounds		=	null;
	[SerializeField] private AudioCollection	_painSounds			=	null;
	[SerializeField] private float				_nextPainSoundTime	=	0.0f;
	[SerializeField] private float				_painSoundOffset	=	0.35f;

	// Private
	private Collider 			_collider 			 = null;
	private FPSController		_fpsController 		 = null;
	private CharacterController _characterController = null;
	private GameSceneManager	_gameSceneManager	 = null;
	private int					_aiBodyPartLayer     = -1;
	private int _interactiveMask = 0; //mask for interactive layer
	private float _nextAttackTime = 0;

	public float health { get { return _health; } }
	public float stamina { get { return _fpsController != null ? _fpsController.stamina : 0.0f; } }
	public FPSController fpsController { get { return _fpsController; } }

	// Use this for initialization
	void Start () 
	{
		_collider 			 = GetComponent<Collider>();
		_fpsController 		 = GetComponent<FPSController>();
		_characterController = GetComponent<CharacterController>();
		_gameSceneManager = GameSceneManager.instance;
		_aiBodyPartLayer = LayerMask.NameToLayer("AI Body Part");
		_interactiveMask = 1 << LayerMask.NameToLayer("Interactive");

		if (_gameSceneManager!=null)
		{
			PlayerInfo info 		= new PlayerInfo();
			info.camera 			= _camera;
			info.characterManager 	= this;
			info.collider			= _collider;
			info.meleeTrigger		= _meleeTrigger;

			_gameSceneManager.RegisterPlayerInfo( _collider.GetInstanceID(), info );
		}

		//remove cursor
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked; //lock the cursor to center 

		//Fade from black screen
		if (_playerHUD) _playerHUD.Fade(2.0f, ScreenFadeType.FadeIn);
	}
	
	public void TakeDamage ( float amount, bool doDamage, bool doPain )
	{
		_health = Mathf.Max ( _health - (amount *Time.deltaTime)  , 0.0f);

		if (_fpsController)
		{
			_fpsController.dragMultiplier = 0.0f; 

		}
		if (_cameraBloodEffect!=null)
		{
			_cameraBloodEffect.minBloodAmount = (1.0f - _health/100.0f) * 0.5f;
			_cameraBloodEffect.bloodAmount = Mathf.Min(_cameraBloodEffect.minBloodAmount + 0.3f, 1.0f);	
		}

		// Do Pain / Damage Sounds
		if (AudioManager.instance)
		{
			if (doDamage && _damageSounds!=null)
				AudioManager.instance.PlayOneShotSound( _damageSounds.audioGroup,
														_damageSounds.audioClip, transform.position,
														_damageSounds.volume,
														_damageSounds.spatialBlend,
														_damageSounds.priority );

			if (doPain && _painSounds!=null && _nextPainSoundTime<Time.time)
			{
				AudioClip painClip = _painSounds.audioClip;
				if (painClip)
				{
					_nextPainSoundTime = Time.time + painClip.length;
					StartCoroutine(AudioManager.instance.PlayOneShotSoundDelayed(	_painSounds.audioGroup,
																			 	 	painClip,
																			  		transform.position,
																			  		_painSounds.volume,
																			  		_painSounds.spatialBlend,
																			  		_painSoundOffset,
																			  		_painSounds.priority ));
				}
			}
		}
	}

	public void DoDamage( int hitDirection = 0 )
	{
		if (_camera==null) return;
		if (_gameSceneManager==null) return;

		// Local Variables
		Ray ray;
		RaycastHit hit;
		bool isSomethingHit	=	false;

		ray = _camera.ScreenPointToRay( new Vector3( Screen.width/2, Screen.height/2, 0 ));

		isSomethingHit = Physics.Raycast( ray, out hit, 1.0f, 1<<_aiBodyPartLayer );

		if (isSomethingHit)
		{
			AIStateMachine stateMachine = _gameSceneManager.GetAIStateMachine( hit.rigidbody.GetInstanceID());
			if (stateMachine)
			{
				stateMachine.TakeDamage( hit.point, ray.direction * 1.0f, 1, hit.rigidbody, this, 0 );
				_nextAttackTime = Time.time + 0.5f;//temporary attack cool down
			}
		}

	}

	void Update()
	{
		Ray ray;
		RaycastHit hit;
		RaycastHit[] hits;

		//process interactive object
		ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

		//calculate ray length
		//horizontal gives 0 from dot product, up or down 90 deg gives 1 or -1
		float rayLength = Mathf.Lerp(1.0f, 1.8f, Mathf.Abs(Vector3.Dot(_camera.transform.forward, Vector3.up)));

		//only hit with given mask
		hits = Physics.RaycastAll(ray, rayLength, _interactiveMask);

		if(hits.Length > 0)
		{
			int highestPriority = int.MinValue;
			InteractiveItem priorityObject = null;

			for(int i = 0; i < hits.Length; ++i)
			{
				hit = hits[i];
				InteractiveItem interactiveObject = _gameSceneManager.GetInteractiveItem(hit.collider.GetInstanceID());

				if(interactiveObject != null && interactiveObject.priority > highestPriority)
				{
					priorityObject = interactiveObject;
					highestPriority = priorityObject.priority;
				}
			}

			if(priorityObject != null)
			{
				if (_playerHUD)
					_playerHUD.SetInteractionText(priorityObject.GetText());

				if (Input.GetButtonDown("Use"))//e
				{
					priorityObject.Activate(this);
				}
			}
		}
		else //no hit
		{
			if (_playerHUD)
				_playerHUD.SetInteractionText(null);
		}

		//check if attack cool down time is passed
		if (Input.GetMouseButtonDown(0) && Time.time > _nextAttackTime)
		{
			DoDamage();
		}

		if (_fpsController && _soundEmitter!=null)
		{
			float newRadius = Mathf.Max( _walkRadius, (100.0f-_health)/_bloodRadiusScale);
			switch (_fpsController.movementStatus)
			{
				case PlayerMoveStatus.Landing: newRadius = Mathf.Max( newRadius, _landingRadius ); break;
				case PlayerMoveStatus.Running: newRadius = Mathf.Max( newRadius, _runRadius ); break;
			}

			_soundEmitter.SetRadius( newRadius );

			_fpsController.dragMultiplierLimit = Mathf.Max(_health/100.0f, 0.25f);
		}

		if (_playerHUD) _playerHUD.Invalidate(this);
	}

	public void DoLevelComplete()
	{
		if (_fpsController)
			_fpsController.freezeMovement = true;

		if (_playerHUD)
		{
			_playerHUD.Fade(4.0f, ScreenFadeType.FadeOut);
			_playerHUD.ShowMissionText("Mission Completed");
			_playerHUD.Invalidate(this);
		}

		//call gameover after 3s
		Invoke("GameOver", 4.0f);
	}

	void GameOver()
	{
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;

		//load main menu
	}

}
