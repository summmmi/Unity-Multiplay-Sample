using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace RainFX
{

public class RainTimeSlider : MonoBehaviour {

public AudioSource audioSource;

private Slider timeSlider;

	void Start () {
		timeSlider = GetComponent<Slider>();
	}
	
	void Update () {
		Time.timeScale = timeSlider.value;
		audioSource.pitch = timeSlider.value;
	}
}
}