import { readonly, ref } from 'vue'
const toasts=ref([]);let nextId=1
export function useToasts(){function remove(id){toasts.value=toasts.value.filter(item=>item.id!==id)}function push({title,message='',type='info',duration=5000}){const id=nextId++;toasts.value.push({id,title,message,type});if(duration>0)setTimeout(()=>remove(id),duration);return id}return{toasts:readonly(toasts),push,remove,success:(title,message)=>push({title,message,type:'success'}),error:(title,message)=>push({title,message,type:'error',duration:8000})}}
