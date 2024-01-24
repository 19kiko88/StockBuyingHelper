import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MainComponent } from './pages/home/components/main/main.component'
import { UserMainComponent } from './pages/users/components/user-main/user-main.component';

const routes: Routes = [
  {path: '', component: MainComponent },
  {path: 'main', component: MainComponent },  
  {path: 'users', component: UserMainComponent },  
  {path: '**', redirectTo: 'main' }//沒有比對到路由
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
