using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamDev.Redis;
using System;

public class TestRedis : MonoBehaviour {
    private RedisDataAccessProvider redis;
    public int count;
    // Use this for initialization
    void Start () {
        redis = new RedisDataAccessProvider();
        redis.Configuration.Host = "192.168.1.4";
        redis.Connect();
            //redis.SendCommand(RedisCommand.SET, "hello", "success!");
            //redis.WaitComplete();
        
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        redis.SendCommand(RedisCommand.GET, "count");
        Debug.Log(redis.ReadString());

    }
}
