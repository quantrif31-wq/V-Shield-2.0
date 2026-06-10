<template>

<div class="container">

<div class="card">

<h2>Quản lý Video Khuôn Mặt</h2>

<!-- ⭐ Chọn nhân viên (admin) -->
<div v-if="isAdmin" class="field">

<label>Chọn nhân viên</label>

<select v-model="selectedEmployeeId" @change="loadVideos">

<option disabled value="">
Vui lòng chọn nhân viên
</option>

<option
v-for="e in employees"
:key="e.employeeId"
:value="e.employeeId"
>

{{ e.fullName }}

</option>

</select>

</div>


<input
type="file"
accept="video/*"
@change="handleFile"
/>


<div v-if="previewUrl" class="preview">

<video
:src="previewUrl"
controls
></video>

</div>


<div v-if="progress > 0" class="progress">

<div
class="bar"
:style="{ width: progress + '%' }"
></div>

</div>


<button
@click="upload"
:disabled="!file || uploading"
>

{{ uploading ? "Đang upload..." : "Upload Video" }}

</button>


<hr>


<h3>Video đã tải lên</h3>


<div
v-for="video in videos"
:key="video.id"
class="video-item"
>

<video
:src="videoPlaybackUrls[video.id] || ''"
controls
></video>

<button
class="delete"
@click="remove(video.id)"
>

Xóa

</button>

</div>

</div>

</div>

</template>

<script>

import { authState } from "../stores/auth"
import { getAll as getEmployees } from "../services/employeeApi"

import {
uploadFaceVideo,
getEmployeeVideos,
getProtectedVideoBlob,
deleteVideo
} from "../services/faceVideoApi"

export default {

data(){

return{

file:null,

previewUrl:null,

progress:0,

uploading:false,

videos:[],

videoPlaybackUrls:{},

employees:[],

selectedEmployeeId:"",

isAdmin:false

}

},

beforeUnmount(){
this.releaseVideoObjectUrls()
},

async mounted(){

const user = authState.user

if(user?.role === "Admin"){

this.isAdmin = true

await this.loadEmployees()

}else{

this.selectedEmployeeId = user.employeeId

}

await this.loadVideos()

},

methods:{


async loadEmployees(){

try{

const res = await getEmployees()
this.employees = res.data

}catch(err){

console.error(err)

}

},


handleFile(e){

const file = e.target.files[0]

if(!file) return

if(file.size > 50*1024*1024){

alert("Video tối đa 50MB")
return

}

this.file = file

this.previewUrl = URL.createObjectURL(file)

},


async upload(){

if(this.isAdmin && !this.selectedEmployeeId){

alert("Vui lòng chọn nhân viên trước khi upload")

return

}

try{

this.uploading = true

await uploadFaceVideo(
this.file,
this.selectedEmployeeId,
(percent)=>{
this.progress = percent
}
)

this.progress=0
this.file=null
this.previewUrl=null

await this.loadVideos()

alert("Upload thành công")

}catch(err){

console.error("UPLOAD ERROR:", err)

let msg = "Upload thất bại"

if(err.response?.data?.message){
    msg = err.response.data.message
}
else if(err.message){
    msg = err.message
}

alert(msg)

}

finally{

this.uploading=false

}

},


async loadVideos(){

if(!this.selectedEmployeeId) return

try{

this.releaseVideoObjectUrls()

const res = await getEmployeeVideos(
this.selectedEmployeeId
)

this.videos = res.data

await this.loadProtectedVideoUrls()

}catch(err){

console.error(err)

}

},

async loadProtectedVideoUrls(){

const entries = await Promise.all(
this.videos.map(async (video) => {
try{
const response = await getProtectedVideoBlob(video.id)
const objectUrl = URL.createObjectURL(response.data)
return [video.id, objectUrl]
}catch(err){
console.error("VIDEO LOAD ERROR:", err)
return [video.id, ""]
}
})
)

this.videoPlaybackUrls = Object.fromEntries(entries)

},


releaseVideoObjectUrls(){

Object.values(this.videoPlaybackUrls).forEach((url) => {
if(url){
URL.revokeObjectURL(url)
}
})

this.videoPlaybackUrls = {}

},


async remove(id){

if(!confirm("Xóa video này?")) return

await deleteVideo(id)

await this.loadVideos()

}

}

}

</script>

<style scoped>

.container{
display:flex;
justify-content:center;
padding:40px;
background:#f4f6fa;
min-height:100vh;
}

.card{
width:650px;
background:white;
padding:30px;
border-radius:12px;
box-shadow:0 10px 30px rgba(0,0,0,0.08);
}

.field{
margin-bottom:15px;
}

select{
width:100%;
padding:8px;
border-radius:6px;
border:1px solid #ddd;
}

.preview video{
width:100%;
margin-top:15px;
border-radius:8px;
}

.progress{
height:10px;
background:#eee;
margin:15px 0;
border-radius:10px;
overflow:hidden;
}

.bar{
height:100%;
background:#22c55e;
}

button{
padding:10px 15px;
border:none;
background:#2563eb;
color:white;
border-radius:6px;
cursor:pointer;
margin-top:10px;
}

button:hover{
background:#1d4ed8;
}

.video-item{
margin-top:20px;
}

.video-item video{
width:100%;
border-radius:8px;
}

.delete{
background:#ef4444;
margin-top:5px;
}

</style>

