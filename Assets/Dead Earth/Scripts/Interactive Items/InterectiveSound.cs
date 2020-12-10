using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterectiveSound : InteractiveItem
{
    [TextArea(3,10)]//allow bigger test field size
    [SerializeField] private string _infoText = null;
    [TextArea(3, 10)]
    [SerializeField] private string _activatedText = null;
    [SerializeField] private float _activatedTextDuration = 3.0f;

    [SerializeField] private AudioCollection _audioCollection = null; //pick one of sounds from collection
    [SerializeField] private int _bank = 0;

    //private
    private IEnumerator _coroutine = null; //sound 
    private float _hideActivatedTextTime = 0.0f; //to measure time elapsed since activated text shown

    public override string GetText()
    {
        if (_coroutine != null || Time.time < _hideActivatedTextTime)
            return _activatedText;
        else
            return _infoText;
    }


    public override void Activate(CharacterManager characterManager)
    {
        if(_coroutine == null)
        {
            _hideActivatedTextTime = Time.time + _activatedTextDuration;
            _coroutine = DoActivation();
            StartCoroutine(_coroutine);
        }
    }

    private IEnumerator DoActivation()
    {
        if (_audioCollection == null || AudioManager.instance == null) yield break;

        AudioClip clip = _audioCollection[_bank];
        if (clip == null) yield break;

        AudioManager.instance.PlayOneShotSound(_audioCollection.audioGroup,
                                                clip,
                                                transform.position,
                                                _audioCollection.volume,
                                                _audioCollection.spatialBlend,
                                                _audioCollection.priority);

        //wait until clip stops
        yield return new WaitForSeconds(clip.length);

        _coroutine = null;
    }
}
