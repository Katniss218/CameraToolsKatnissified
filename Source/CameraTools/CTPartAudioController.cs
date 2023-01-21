using UnityEngine;

namespace CameraToolsKatnissified
{
    public class CTPartAudioController : MonoBehaviour
    {
        Vessel _vessel;
        Part _part;

        public AudioSource audioSource;

        float _origMinDist = 1;
        float _origMaxDist = 1;

        float _modMinDist = 10;
        float _modMaxDist = 10000;

        AudioRolloffMode _origRolloffMode;

        void Awake()
        {
            _part = GetComponentInParent<Part>();
            _vessel = _part.vessel;

            CamTools.OnResetCTools += OnResetCTools;
        }

        void Start()
        {
            if( !audioSource )
            {
                Destroy( this );
                return;
            }

            _origMinDist = audioSource.minDistance;
            _origMaxDist = audioSource.maxDistance;
            _origRolloffMode = audioSource.rolloffMode;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.spatialBlend = 1;

        }

        void FixedUpdate()
        {
            if( !audioSource )
            {
                Destroy( this );
                return;
            }

            if( !_part || !_vessel )
            {
                Destroy( this );
                return;
            }


            float angleToCam = Vector3.Angle( _vessel.srf_velocity, FlightCamera.fetch.mainCamera.transform.position - _vessel.transform.position );
            angleToCam = Mathf.Clamp( angleToCam, 1, 180 );

            float srfSpeed = (float)_vessel.srfSpeed;
            srfSpeed = Mathf.Min( srfSpeed, 550f );

            float lagAudioFactor = (75000 / (Vector3.Distance( _vessel.transform.position, FlightCamera.fetch.mainCamera.transform.position ) * srfSpeed * angleToCam / 90));
            lagAudioFactor = Mathf.Clamp( lagAudioFactor * lagAudioFactor * lagAudioFactor, 0, 4 );
            lagAudioFactor += srfSpeed / 230;

            float waveFrontFactor = ((3.67f * angleToCam) / srfSpeed);
            waveFrontFactor = Mathf.Clamp( waveFrontFactor * waveFrontFactor * waveFrontFactor, 0, 2 );
            if( _vessel.srfSpeed > CamTools.speedOfSound )
            {
                waveFrontFactor = (srfSpeed / (angleToCam) < 3.67f) ? waveFrontFactor + ((srfSpeed / (float)CamTools.speedOfSound) * waveFrontFactor) : 0;
            }

            lagAudioFactor *= waveFrontFactor;

            audioSource.minDistance = Mathf.Lerp( _origMinDist, _modMinDist * lagAudioFactor, Mathf.Clamp01( (float)_vessel.srfSpeed / 30 ) );
            audioSource.maxDistance = Mathf.Lerp( _origMaxDist, Mathf.Clamp( _modMaxDist * lagAudioFactor, audioSource.minDistance, 16000 ), Mathf.Clamp01( (float)_vessel.srfSpeed / 30 ) );

        }

        void OnDestroy()
        {
            CamTools.OnResetCTools -= OnResetCTools;
        }

        void OnResetCTools()
        {
            audioSource.minDistance = _origMinDist;
            audioSource.maxDistance = _origMaxDist;
            audioSource.rolloffMode = _origRolloffMode;
            Destroy( this );
        }
    }
}