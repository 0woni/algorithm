#include <iostream>
#include <cstdlib>
#include<ctime>
// 블로그 참고 https://blockdmask.tistory.com/308
using namespace std;

void merge(int h, int m, const int U[], const int V[], int S[]) {
    int i=0, j=0, k=0;
    while (i < h && j < m) {
        if (U[i] < V[j]) {
            S[k] = U[i];
            i++;
        }
        else {
            S[k] = V[j];
            j++;
        }
        k++;
    }
    while (i < h) S[k++] = U[i++];
    while (j < m) S[k++] = V[j++];
    // While문 자체가 하나의 조건식이기 때문에 if문 생략이 가능해짐
}

void mergesort(int n, int S[]) {
    const int h = n / 2, m = n - h;
    if (n > 1) {
        int* U = new int[h];
        int* V = new int[m];

        for (int i = 0; i < h; i++) {
            U[i] = S[i];
        }
        for (int i = 0; i < m; i++) {
            V[i] = S[h + i];
        }
        mergesort(h, U);
        mergesort(m, V);

        merge(h, m, U, V, S);
    }
}

int main()
{
    int a = 20;
    int S[20];

    srand(static_cast<unsigned int>(time(0)));

    cout << "원본 배열 : ";
    for (int i = 0; i < a; i++) {
        S[i] = rand() % 100;
        cout << "[" << S[i] << "], ";
    }
    cout << endl;

    mergesort(a, S);

    cout << "정렬된 배열 : ";
    for (int i = 0; i < a; i++) {
        cout << "[" << S[i] << "], ";
    }
    cout << endl;
}

