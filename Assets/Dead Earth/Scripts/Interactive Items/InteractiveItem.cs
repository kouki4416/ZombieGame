using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Base class for all interactive item
public class InteractiveItem : MonoBehaviour
{   
    //need in the case more than 2 items at the same place
    [SerializeField] protected int _priority = 0;
    public int priority { get { return _priority; } }

    protected GameSceneManager _gameSceneManager = null;
    protected Collider _collider = null;

    //return text to be displayed
    public virtual string GetText() { return null; }

    public virtual void Activate (CharacterManager characterManager) { }

    protected virtual void Start()
    {
        _gameSceneManager = GameSceneManager.instance;
        _collider = GetComponent<Collider>();

        if(_gameSceneManager != null && _collider != null)
        {
            //register item id in gameSceneManager so that you don't have to call costly GetComponent<>
            _gameSceneManager.RegisterInteractiveItem(_collider.GetInstanceID(), this);
        }
    }
}
