import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MainComponent } from './pages/home/components/main/main.component'
import { LoginComponent } from './pages/login/components/login/login.component';

const routes: Routes = [
  {path: '', component: MainComponent },
  {path: 'login', component: LoginComponent },
  {path: 'main', component: MainComponent },
  {
    path: 'admin',
    loadChildren: () => import('./pages/admin/admin.module').then(m => m.AdminModule)
  },
  {path: '**', redirectTo: 'main' },//沒有比對到路由，要放到最後面
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
