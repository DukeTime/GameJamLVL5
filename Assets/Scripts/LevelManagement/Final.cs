using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LevelManagement
{
    public class Final:MonoBehaviour
    {
        public string dialogue;
        public GameObject a;
        public GameObject b;

        private void Start()
        {
            DialogSystem.Instance.LoadDialog(dialogue);
            //StartCoroutine(sd());
        }

        private IEnumerator sd()
        {
            yield return new WaitForSeconds(4);
            a.SetActive(false);
            b.SetActive(true);
        }
    }
}