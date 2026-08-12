package com.vshield.mobile.data

import com.vshield.mobile.BuildConfig
import com.vshield.mobile.data.model.RefreshTokenRequest
import kotlinx.coroutines.runBlocking
import okhttp3.Authenticator
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.Response
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object RetrofitClient {

    private var authToken: String? = null
    private var tokenManager: TokenManager? = null

    fun init(manager: TokenManager) {
        tokenManager = manager
        authToken = manager.getToken()
    }

    fun setToken(token: String?) {
        authToken = token
    }

    fun getToken(): String? = authToken

    private val authInterceptor = Interceptor { chain ->
        val request = chain.request().newBuilder()
        authToken?.let {
            request.addHeader("Authorization", "Bearer $it")
        }
        request.addHeader("Content-Type", "application/json")
        chain.proceed(request.build())
    }

    private val loggingInterceptor = HttpLoggingInterceptor().apply {
        level = if (BuildConfig.DEBUG) {
            HttpLoggingInterceptor.Level.BODY
        } else {
            HttpLoggingInterceptor.Level.NONE
        }
    }

    private fun responseCount(response: Response): Int {
        var count = 1
        var priorResponse = response.priorResponse
        while (priorResponse != null) {
            count++
            priorResponse = priorResponse.priorResponse
        }
        return count
    }

    private fun shouldRefresh(response: Response): Boolean {
        if (response.code != 401) return false
        if (responseCount(response) >= 2) return false

        val path = response.request.url.encodedPath.lowercase()
        return !path.contains("/api/auth/login") && !path.contains("/api/auth/refresh")
    }

    private fun refreshAccessToken(): String? = runBlocking {
        val manager = tokenManager ?: return@runBlocking null
        val refreshToken = manager.getRefreshToken() ?: return@runBlocking null

        return@runBlocking try {
            val response = refreshApiService.refresh(RefreshTokenRequest(refreshToken))
            val payload = response.body()

            if (!response.isSuccessful || payload == null || payload.token.isBlank()) {
                manager.clear()
                authToken = null
                null
            } else {
                authToken = payload.token
                manager.saveToken(payload.token)
                payload.refreshToken?.let { manager.saveRefreshToken(it) }
                payload.employeeId?.let { manager.saveEmployeeId(it) }
                payload.role?.let { manager.saveRoles(setOf(it)) }
                payload.token
            }
        } catch (_: Exception) {
            null
        }
    }

    private val authenticator = Authenticator { _, response ->
        if (!shouldRefresh(response)) {
            return@Authenticator null
        }

        synchronized(this) {
            val currentToken = authToken
            val requestAuthHeader = response.request.header("Authorization")
            if (!currentToken.isNullOrBlank() && requestAuthHeader != "Bearer $currentToken") {
                return@synchronized response.request.newBuilder()
                    .header("Authorization", "Bearer $currentToken")
                    .build()
            }

            val nextToken = refreshAccessToken() ?: return@synchronized null
            response.request.newBuilder()
                .header("Authorization", "Bearer $nextToken")
                .build()
        }
    }

    private val okHttpClient = OkHttpClient.Builder()
        .addInterceptor(authInterceptor)
        .addInterceptor(loggingInterceptor)
        .authenticator(authenticator)
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .writeTimeout(30, TimeUnit.SECONDS)
        .build()

    private val refreshClient = OkHttpClient.Builder()
        .addInterceptor(Interceptor { chain ->
            val request = chain.request().newBuilder()
                .addHeader("Content-Type", "application/json")
                .build()
            chain.proceed(request)
        })
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .writeTimeout(30, TimeUnit.SECONDS)
        .build()

    private val retrofit = Retrofit.Builder()
        .baseUrl(BuildConfig.API_BASE_URL + "/")
        .client(okHttpClient)
        .addConverterFactory(GsonConverterFactory.create())
        .build()

    private val refreshRetrofit = Retrofit.Builder()
        .baseUrl(BuildConfig.API_BASE_URL + "/")
        .client(refreshClient)
        .addConverterFactory(GsonConverterFactory.create())
        .build()

    val apiService: ApiService =
        if (BuildConfig.DEMO_MODE) {
            DemoApiService
        } else {
            retrofit.create(ApiService::class.java)
        }
    private val refreshApiService: ApiService = refreshRetrofit.create(ApiService::class.java)
}
